using System;
using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class CarteiraService
{
    private readonly Client _supabaseClient;
    private readonly IConfiguration _configuration;
    private readonly UsuarioService _usuarioService;
    private readonly NotificacaoService _notificacaoService;

    public CarteiraService(SupabaseService supabaseService, IConfiguration configuration, UsuarioService usuarioService, NotificacaoService notificacaoService)
    {
        _supabaseClient = supabaseService.GetClient();
        _configuration = configuration;
        _usuarioService = usuarioService;
        _notificacaoService = notificacaoService;
    }

    public async Task<(bool success, string message, CarteiraResponseDTO? carteira)> ObterCarteira(string token)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;
            
            if (!success)
            {
                return (false, message, null);
            }

            var response = await _supabaseClient
                .From<CarteiraDigital>()
                .Select("*")
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (response == null)
            {
                var novaCarteira = new CarteiraDigital
                {
                    UsuarioId = userId,
                    Pontos = 0,
                    Saldo = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _supabaseClient
                    .From<CarteiraDigital>()
                    .Insert(novaCarteira);

                return (true, "Carteira criada com sucesso", new CarteiraResponseDTO
                {
                    Id = novaCarteira.Id,
                    CreatedAt = novaCarteira.CreatedAt,
                    Pontos = novaCarteira.Pontos,
                    Saldo = novaCarteira.Saldo,
                    UsuarioId = novaCarteira.UsuarioId
                });
            }

            return (true, "Carteira obtida com sucesso", new CarteiraResponseDTO
            {
                Id = response.Id,
                CreatedAt = response.CreatedAt,
                Pontos = response.Pontos,
                Saldo = response.Saldo,
                UsuarioId = response.UsuarioId
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter carteira: {ex.Message}");
            return (false, $"Erro ao obter carteira digital: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message)> Transferir(TransferenciaDTO dto)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(dto.Token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;
            
            if (!success)
            {
                return (false, message);
            }

            var remetente = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();

            if (remetente == null)
            {
                return (false, "Remetente não encontrado");
            }

            var destinatario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.UserUid == dto.UidDestinatario)
                .Single();

            if (destinatario == null)
            {
                return (false, "Destinatário não encontrado");
            }

            var carteiraRemetente = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteiraRemetente == null)
            {
                return (false, "Carteira do remetente não encontrada");
            }

            var carteiraDestinatario = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == destinatario.Id)
                .Single();

            if (carteiraDestinatario == null)
            {
                carteiraDestinatario = new CarteiraDigital
                {
                    UsuarioId = destinatario.Id,
                    Pontos = 0,
                    Saldo = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _supabaseClient
                    .From<CarteiraDigital>()
                    .Insert(carteiraDestinatario);
            }

            if (dto.Tipo.ToLower() == "saldo")
            {
                if (carteiraRemetente.Saldo < dto.Valor)
                {
                    return (false, "Saldo insuficiente");
                }

                carteiraRemetente.Saldo -= dto.Valor;
                carteiraDestinatario.Saldo += dto.Valor;
            }
            else
            {
                var valorInteiro = (long)dto.Valor;
                if (carteiraRemetente.Pontos < valorInteiro)
                {
                    return (false, "Pontos insuficientes");
                }

                carteiraRemetente.Pontos -= valorInteiro;
                carteiraDestinatario.Pontos += valorInteiro;
            }

            await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.Id == carteiraRemetente.Id)
                .Update(carteiraRemetente);

            await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.Id == carteiraDestinatario.Id)
                .Update(carteiraDestinatario);

            var mensagemNotificacao = dto.Tipo.ToLower() == "saldo"
                ? $"Você recebeu uma transferência de {dto.Valor} em saldo de {remetente.Nome}!"
                : $"Você recebeu uma transferência de {(long)dto.Valor} pontos de {remetente.Nome}!";
            var (notificacaoSuccess, notificacaoMessage) = await _notificacaoService.CriarNotificacaoPessoal(
                usuarioId: destinatario.Id,
                mensagem: mensagemNotificacao,
                tipo: "Transferência Recebida",
                dataExpiracao: DateTime.UtcNow.AddDays(7)
            );

            if (!notificacaoSuccess)
            {
                Console.WriteLine($"Falha ao criar notificação para transferência: {notificacaoMessage}");
            }

            return (true, "Transferência realizada com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao realizar transferência: {ex.Message}");
            return (false, $"Erro ao realizar transferência: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> TrocarPontosPorSaldo(string token, long pontos)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return (false, "Token não pode ser nulo ou vazio.");
            }

            if (pontos <= 0 || pontos % 2 != 0)
            {
                return (false, "A quantidade de pontos deve ser um número positivo múltiplo de 2.");
            }

            if (pontos < 4000)
            {
                return (false, "É necessário ter pelo menos 4000 pontos para realizar a troca.");
            }

            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message);
            }
            long userId = validationResult.userId;

            var carteira = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteira == null)
            {
                return (false, "Carteira do usuário não encontrada.");
            }

            if (carteira.Pontos < pontos)
            {
                return (false, "Pontos insuficientes para a troca.");
            }

            var hoje = DateTime.UtcNow.Date;
            var amanha = hoje.AddDays(1);

            var trocasHoje = await _supabaseClient
                .From<TrocaPontos>()
                .Match(new Dictionary<string, string> { { "usuario_id", userId.ToString() } })
                .Filter("data_troca", Operator.GreaterThanOrEqual, hoje.ToString("yyyy-MM-dd"))
                .Filter("data_troca", Operator.LessThan, amanha.ToString("yyyy-MM-dd"))
                .Get();

            if (trocasHoje.Models.Count >= 3)
            {
                return (false, "Limite de 3 trocas por dia atingido.");
            }

            decimal saldo = pontos / 2m;

            carteira.Pontos -= pontos;
            carteira.Saldo += saldo;

            await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.Id == carteira.Id)
                .Update(carteira);

            var novaTroca = new TrocaPontos
            {
                UsuarioId = userId,
                PontosTrocados = pontos,
                SaldoObtido = saldo,
                DataTroca = DateTime.UtcNow
            };

            await _supabaseClient
                .From<TrocaPontos>()
                .Insert(novaTroca);

            return (true, "Troca realizada com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao trocar pontos por saldo: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return (false, $"Erro ao trocar pontos por saldo: {ex.Message}");
        }
    }
}