using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class ConquistasService
{
    private readonly SupabaseService _supabaseService;
    private readonly UsuarioService _usuarioService;
    private readonly ILogger<ConquistasService> _logger;
    private readonly IConfiguration _configuration;
    private readonly NotificacaoService _notificacaoService; // Adicionado

    public ConquistasService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        ILogger<ConquistasService> logger,
        IConfiguration configuration,
        NotificacaoService notificacaoService) // Adicionado
    {
        _supabaseService = supabaseService;
        _usuarioService = usuarioService;
        _logger = logger;
        _configuration = configuration;
        _notificacaoService = notificacaoService; // Adicionado
    }

    public async Task CheckAndAssignAchievements(long userId)
    {
        try
        {
            var client = _supabaseService.GetClient();

            var conquistasResponse = await client.From<Conquista>().Get();
            var conquistas = conquistasResponse.Models;

            var conquistasUsuarioResponse = await client.From<ConquistasUsuarios>()
                .Where(cu => cu.UsuarioId == userId)
                .Get();
            var conquistasAtribuidas = conquistasUsuarioResponse.Models.Select(cu => cu.ConquistaId).ToList();

            foreach (var conquista in conquistas)
            {
                if (conquistasAtribuidas.Contains(conquista.Id))
                    continue;

                bool conquistou = false;
                switch (conquista.Id)
                {
                    case 1: conquistou = await CheckInicianteSustentavel(userId); break;
                    case 2: conquistou = await CheckConsistenciaVerde(userId); break;
                    case 3: conquistou = await CheckMestreReciclador(userId); break;
                    case 4: conquistou = await CheckCampeaoCampanhas(userId); break;
                    case 5: conquistou = await CheckGuardiaoMeioAmbiente(userId); break;
                    default:
                        _logger.LogWarning($"Conquista com ID {conquista.Id} não tem verificação implementada.");
                        continue;
                }

                if (conquistou)
                {
                    var conquistaUsuario = new ConquistasUsuarios
                    {
                        ConquistaId = conquista.Id,
                        UsuarioId = userId,
                        DataConquista = DateTime.UtcNow
                    };
                    await client.From<ConquistasUsuarios>().Insert(conquistaUsuario);

                    await _usuarioService.AtualizarPontos(userId, conquista.Pontos);

                    // Criar notificação para a conquista
                    var mensagem = $"Parabéns! Você conquistou '{conquista.Nome}' e ganhou {conquista.Pontos} pontos!";
                    var (success, message) = await _notificacaoService.CriarNotificacaoPessoal(
                        usuarioId: userId,
                        mensagem: mensagem,
                        tipo: "Conquista Obtida",
                        dataExpiracao: DateTime.UtcNow.AddDays(7) // Expira em 7 dias
                    );

                    if (!success)
                    {
                        _logger.LogWarning($"Falha ao criar notificação para conquista {conquista.Id}: {message}");
                    }

                    _logger.LogInformation($"Conquista {conquista.Nome} (ID: {conquista.Id}) atribuída ao usuário {userId}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao verificar e atribuir conquistas para o usuário {userId}.");
            throw;
        }
    }

    // Verificação para "Iniciante Sustentável"
    private async Task<bool> CheckInicianteSustentavel(long userId)
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Reciclagem>()
            .Where(r => r.UsuarioId == userId)
            .Get();
        return response.Models.Any();
    }

    // Verificação para "Consistência Verde"
    private async Task<bool> CheckConsistenciaVerde(long userId)
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Reciclagem>()
            .Where(r => r.UsuarioId == userId)
            .Select("created_at")
            .Order("created_at", Ordering.Ascending)
            .Get();
        var dates = response.Models
            .Select(r => r.CreatedAt.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        for (int i = 0; i <= dates.Count - 7; i++)
        {
            if ((dates[i + 6] - dates[i]).Days == 6)
            {
                return true;
            }
        }
        return false;
    }

    // Verificação para "Mestre Reciclador"
    private async Task<bool> CheckMestreReciclador(long userId)
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Reciclagem>()
            .Where(r => r.UsuarioId == userId)
            .Select("peso")
            .Get();
        var totalPeso = response.Models.Sum(r => r.Peso);
        return totalPeso >= 100;
    }

    // Verificação para "Campeão de Campanhas"
    private async Task<bool> CheckCampeaoCampanhas(long userId)
{
    var client = _supabaseService.GetClient();

    // Obter campanhas ativas
    var activeCampaignsResponse = await client.From<Campanha>()
        .Filter("status", Operator.Equals, "Ativo")
        .Filter("data_inicio", Operator.LessThanOrEqual, DateTime.UtcNow.ToString("yyyy-MM-dd"))
        .Filter("data_fim", Operator.GreaterThanOrEqual, DateTime.UtcNow.ToString("yyyy-MM-dd"))
        .Select("id")
        .Get();

    var activeCampaignIds = activeCampaignsResponse.Models.Select(c => c.Id).ToList();

    if (activeCampaignIds.Count == 0)
        return false;

    // Obter participações do usuário em campanhas ativas
    var participationsResponse = await client.From<UsuarioCampanha>()
        .Filter("usuario_id", Operator.Equals, userId)
        .Filter("campanha_id", Operator.In, activeCampaignIds) // Passar a lista diretamente
        .Get();

    var participations = participationsResponse.Models;

    // Verificar se há pelo menos 5 participações (status "Pendente" ou "Completa")
    if (participations.Count < 5)
        return false;

    // Contar campanhas concluídas (status "Completa")
    var completedCampaigns = participations.Count(uc => uc.Status == "Completa");

    return completedCampaigns >= 3;
}

    // Verificação para "Guardião do Meio Ambiente"
    private async Task<bool> CheckGuardiaoMeioAmbiente(long userId)
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Reciclagem>()
            .Where(r => r.UsuarioId == userId)
            .Select("peso")
            .Get();
        var totalPeso = response.Models.Sum(r => r.Peso);
        return totalPeso >= 50;
    }

    // Listar todas as conquistas disponíveis
    public async Task<List<Conquista>> GetAllConquistas()
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Conquista>().Get();
        return response.Models;
    }

    // Listar conquistas do usuário// Listar conquistas do usuário
    public async Task<List<ConquistasUsuarios>> GetUserConquistas(long userId)
{
    var client = _supabaseService.GetClient();
    var response = await client.From<ConquistasUsuarios>()
        .Select("*") // Inclui a tabela relacionada
        .Filter("usuario_id", Operator.Equals, Convert.ToInt32(userId)) // Filtra pelo usuario_id
        .Get();
    
    return response.Models;
}
}