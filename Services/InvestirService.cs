using System;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class InvestirService
{
    private readonly SupabaseService _supabaseService;
    private readonly UsuarioService _usuarioService;
    private readonly InvestimentoService _investimentoService;
    private readonly ILogger<InvestirService> _logger;

    public InvestirService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        InvestimentoService investimentoService,
        ILogger<InvestirService> logger)
    {
        _supabaseService = supabaseService;
        _usuarioService = usuarioService;
        _investimentoService = investimentoService;
        _logger = logger;
    }

    public async Task<(bool success, string message)> MakeInvestment(long userId, long investimentoId, long pontosInvestidos)
    {
        try
        {
            // Validar o investimento
            var (isValid, validationMessage) = await ValidateInvestment(userId, investimentoId, pontosInvestidos);
            if (!isValid)
            {
                return (false, validationMessage);
            }

            var client = _supabaseService.GetClient();
            var investment = await _investimentoService.GetInvestmentById(investimentoId);
            if (investment == null || investment.Status != "Ativo")
            {
                _logger.LogError("Investimento {InvestimentoId} não está ativo ou não existe", investimentoId);
                return (false, "Investimento não está ativo ou não existe");
            }

            // Calcular taxa de retorno e período
            var returnRate = GetReturnRate(pontosInvestidos);
            var timePeriod = GetTimePeriod(pontosInvestidos);
            var returnDate = CalculateReturnDate(timePeriod);
            var returnAmount = CalculateReturnAmount(pontosInvestidos, returnRate);

            // Deduzir pontos da carteira do usuário
            var (deductSuccess, deductMessage, _) = await _usuarioService.DeductPoints(userId, pontosInvestidos);
            if (!deductSuccess)
            {
                _logger.LogWarning("Falha ao deduzir pontos para o usuário {UserId}: {Message}", userId, deductMessage);
                return (false, deductMessage);
            }

            // Criar novo registro de investimento
            var newInvestir = new Investir
            {
                Id = DateTime.UtcNow.Ticks, // Gerar um ID único
                CreatedAt = DateTime.UtcNow,
                PontosInvestidos = pontosInvestidos,
                UsuarioId = userId,
                InvestimentoId = investimentoId,
                DataRetorno = returnDate,
                ValorRetorno = returnAmount
            };
            await client.From<Investir>().Insert(newInvestir);

            // Atualizar total_investido no investimento
            var updatedTotalInvestido = investment.TotalInvestido + pontosInvestidos;
            await client.From<Investimento>()
                .Where(i => i.Id == investimentoId)
                .Set(i => i.TotalInvestido, updatedTotalInvestido)
                .Update();

            // Verificar se o investimento atingiu a meta
            await _investimentoService.UpdateInvestmentStatus(investimentoId);

            _logger.LogInformation("Investimento realizado com sucesso para o usuário {UserId} no investimento {InvestimentoId}", userId, investimentoId);
            return (true, "Investimento realizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar investimento para o usuário {UserId}", userId);
            return (false, $"Erro ao realizar investimento: {ex.Message}");
        }
    }

    private double GetReturnRate(long pontosInvestidos)
    {
        if (pontosInvestidos >= 9000 && pontosInvestidos <= 60000)
            return 0.25; // 25% de lucro
        else if (pontosInvestidos >= 60001 && pontosInvestidos <= 100000)
            return 0.55; // 55% de lucro
        else
            throw new Exception("Valor de investimento inválido");
    }

    private TimeSpan GetTimePeriod(long pontosInvestidos)
    {
        if (pontosInvestidos >= 9000 && pontosInvestidos <= 60000)
            return TimeSpan.FromDays(180); // 6 meses
        else if (pontosInvestidos >= 60001 && pontosInvestidos <= 100000)
            return TimeSpan.FromDays(365); // 1 ano
        else
            throw new Exception("Valor de investimento inválido");
    }

    private DateTime CalculateReturnDate(TimeSpan timePeriod)
    {
        return DateTime.UtcNow.Add(timePeriod);
    }

    private long CalculateReturnAmount(long pontosInvestidos, double returnRate)
    {
        return Convert.ToInt64(pontosInvestidos * (1 + returnRate));
    }

    private async Task<(bool isValid, string message)> ValidateInvestment(long userId, long investimentoId, long pontosInvestidos)
    {
        try
        {
            // Verificar se o usuário tem pontos suficientes
            var (walletSuccess, walletMessage, pontos) = await _usuarioService.GetUserWallet(userId);
            if (!walletSuccess)
            {
                _logger.LogWarning("Erro ao obter carteira do usuário {UserId}: {Message}", userId, walletMessage);
                return (false, walletMessage);
            }

            if (pontos < pontosInvestidos)
            {
                _logger.LogWarning("Usuário {UserId} não tem pontos suficientes para investir", userId);
                return (false, "Usuário não tem pontos suficientes para investir");
            }

            // Verificar se o valor está dentro do intervalo permitido
            if (pontosInvestidos < 9000 || pontosInvestidos > 100000)
            {
                _logger.LogWarning("Valor de investimento fora do intervalo permitido para o usuário {UserId}", userId);
                return (false, "Valor de investimento fora do intervalo permitido (9000 a 100.000 pontos)");
            }

            // Verificar se o investimento existe e está ativo
            var investment = await _investimentoService.GetInvestmentById(investimentoId);
            if (investment == null || investment.Status != "Ativo")
            {
                _logger.LogWarning("Investimento {InvestimentoId} não está ativo ou não existe", investimentoId);
                return (false, "Investimento não está ativo ou não existe");
            }

            // Verificar se o usuário já investiu neste investimento neste mês
            var client = _supabaseService.GetClient();
            var existingInvestments = await client.From<Investir>()
                .Where(i => i.UsuarioId == userId && i.InvestimentoId == investimentoId)
                .Filter("created_at", Operator.GreaterThanOrEqual, new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("o"))
                .Get();
            if (existingInvestments.Models.Any())
            {
                _logger.LogWarning("Usuário {UserId} já investiu neste investimento neste mês", userId);
                return (false, "Usuário já investiu neste investimento neste mês");
            }

            return (true, "Validação bem-sucedida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar investimento para o usuário {UserId}", userId);
            return (false, $"Erro ao validar investimento: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> ApplyReturns(string token)
    {
        try
        {
            // Validar o token e obter o ID do usuário logado
            var (tokenValid, tokenMessage, loggedUserId) = await _usuarioService.ValidateToken(token);
            if (!tokenValid)
            {
                _logger.LogWarning("Token inválido: {Message}", tokenMessage);
                return (false, tokenMessage);
            }

            var client = _supabaseService.GetClient();

            // Buscar todos os registros da tabela 'investir' para o usuário logado
            var dueInvestments = await client.From<Investir>()
                .Where(i => i.UsuarioId == loggedUserId) // Filtrar pelo usuário logado
                .Filter("data_retorno", Operator.LessThanOrEqual, DateTime.UtcNow.ToString("o")) // Data de retorno <= data atual
                .Get();

            if (!dueInvestments.Models.Any())
            {
                _logger.LogInformation("Nenhum investimento com retorno devido encontrado para o usuário {UserId}", loggedUserId);
                return (true, "Nenhum retorno a ser aplicado no momento");
            }

            foreach (var investment in dueInvestments.Models)
            {
                // Verificar se o valor_retorno é maior que zero
                if (investment.ValorRetorno <= 0)
                {
                    _logger.LogInformation("Investimento {InvestimentoId} do usuário {UserId} tem valor_retorno zero ou negativo, pulando aplicação", 
                        investment.InvestimentoId, loggedUserId);
                    continue; // Pula para o próximo registro
                }

                // Adicionar os pontos à carteira digital usando o método AtualizarPontos
                var (success, message, _) = await _usuarioService.AtualizarPontos(investment.UsuarioId, Convert.ToInt32(investment.ValorRetorno));
                if (!success)
                {
                    _logger.LogWarning("Falha ao aplicar retorno para o usuário {UserId}: {Message}", investment.UsuarioId, message);
                    continue; // Pula para o próximo registro em caso de falha
                }

                // Atualizar a coluna valor_retorno para 0
                await client.From<Investir>()
                    .Where(i => i.Id == investment.Id)
                    .Set(i => i.ValorRetorno, 0.0)
                    .Update();

                _logger.LogInformation("Retorno de {ValorRetorno} pontos aplicado com sucesso para o usuário {UserId} no investimento {InvestimentoId}",
                    investment.ValorRetorno, investment.UsuarioId, investment.InvestimentoId);
            }

            return (true, "Retornos aplicados com sucesso para os investimentos elegíveis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aplicar retornos para o usuário logado");
            return (false, $"Erro ao aplicar retornos: {ex.Message}");
        }
    }
}