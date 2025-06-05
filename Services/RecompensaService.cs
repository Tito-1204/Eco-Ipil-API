using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Responses;
using System.Linq;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class RecompensaService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly CarteiraService _carteiraService;
    private readonly TicketService _ticketService;
    private readonly NotificacaoService _notificacaoService;

    public RecompensaService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        CarteiraService carteiraService,
        TicketService ticketService,
        NotificacaoService notificacaoService)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _carteiraService = carteiraService;
        _ticketService = ticketService;
        _notificacaoService = notificacaoService;
    }

    /// <summary>
    /// Lista todas as recompensas disponíveis com filtros opcionais (requer autenticação)
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaResponseDTO>? recompensas)> ListarRecompensas(
        string token,
        string? tipo = null,
        long? precoMin = null,
        long? precoMax = null)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            // Iniciar a consulta
            var query = _supabaseClient.From<Recompensa>().Select("*");

            // Aplicar filtros
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Filter("tipo", Operator.Equals, tipo);
            }

            if (precoMin.HasValue)
            {
                query = query.Filter("pontos", Operator.GreaterThanOrEqual, (int)precoMin.Value);
            }

            if (precoMax.HasValue)
            {
                query = query.Filter("pontos", Operator.LessThanOrEqual, (int)precoMax.Value);
            }

            // Executar a consulta
            var recompensas = await query.Get();

            if (recompensas == null || !recompensas.Models.Any())
            {
                return (true, "Nenhuma recompensa encontrada", new List<RecompensaResponseDTO>());
            }

            // Mapear para o DTO de resposta
            var recompensasDTO = recompensas.Models.Select(r => new RecompensaResponseDTO
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                Nome = r.Nome,
                Tipo = r.Tipo,
                Descricao = r.Descricao,
                Pontos = r.Pontos,
                QtRestante = r.QtRestante
            }).ToList();

            return (true, "Recompensas listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas: {ex.Message}");
            return (false, $"Erro ao listar recompensas: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Lista todas as recompensas disponíveis sem exigir autenticação (usado para notificações gerais)
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaResponseDTO>? recompensas)> ListarRecompensasSemAutenticacao(
        string? tipo = null,
        long? precoMin = null,
        long? precoMax = null)
    {
        try
        {
            // Iniciar a consulta
            var query = _supabaseClient.From<Recompensa>().Select("*");

            // Aplicar filtros
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Filter("tipo", Operator.Equals, tipo);
            }

            if (precoMin.HasValue)
            {
                query = query.Filter("pontos", Operator.GreaterThanOrEqual, (int)precoMin.Value);
            }

            if (precoMax.HasValue)
            {
                query = query.Filter("pontos", Operator.LessThanOrEqual, (int)precoMax.Value);
            }

            // Executar a consulta
            var recompensas = await query.Get();

            if (recompensas == null || !recompensas.Models.Any())
            {
                return (true, "Nenhuma recompensa encontrada", new List<RecompensaResponseDTO>());
            }

            // Mapear para o DTO de resposta
            var recompensasDTO = recompensas.Models.Select(r => new RecompensaResponseDTO
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                Nome = r.Nome,
                Tipo = r.Tipo,
                Descricao = r.Descricao,
                Pontos = r.Pontos,
                QtRestante = r.QtRestante
            }).ToList();

            return (true, "Recompensas listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas sem autenticação: {ex.Message}");
            return (false, $"Erro ao listar recompensas sem autenticação: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Obtém os detalhes de uma recompensa específica
    /// </summary>
    public async Task<(bool success, string message, RecompensaResponseDTO? recompensa)> ObterRecompensa(
        string token,
        long recompensaId)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            // Buscar a recompensa
            var recompensa = await _supabaseClient
                .From<Recompensa>()
                .Where(r => r.Id == recompensaId)
                .Single();

            if (recompensa == null)
            {
                return (false, "Recompensa não encontrada", null);
            }

            // Mapear para o DTO de resposta
            var recompensaDTO = new RecompensaResponseDTO
            {
                Id = recompensa.Id,
                CreatedAt = recompensa.CreatedAt,
                Nome = recompensa.Nome,
                Tipo = recompensa.Tipo,
                Descricao = recompensa.Descricao,
                Pontos = recompensa.Pontos,
                QtRestante = recompensa.QtRestante
            };

            return (true, "Recompensa encontrada com sucesso", recompensaDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter recompensa: {ex.Message}");
            return (false, $"Erro ao obter recompensa: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Resgata uma recompensa para o usuário
    /// </summary>
   public async Task<(bool success, string message, RecompensaResgateResponseDTO? data)> ResgatarRecompensa(
        string token,
        long recompensaId)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            long userId = validationResult.userId;

            var recompensa = await _supabaseClient
                .From<Recompensa>()
                .Where(r => r.Id == recompensaId)
                .Single();

            if (recompensa == null)
            {
                return (false, "Recompensa não encontrada", null);
            }

            if (recompensa.QtRestante <= 0)
            {
                return (false, "Esta recompensa não está mais disponível (estoque esgotado)", null);
            }

            var carteiraResult = await _carteiraService.ObterCarteira(token);
            if (!carteiraResult.success || carteiraResult.carteira == null)
            {
                return (false, "Não foi possível verificar sua carteira digital", null);
            }

            var carteira = carteiraResult.carteira;
            if (carteira.Pontos < recompensa.Pontos)
            {
                return (false, $"Pontos insuficientes. Você possui {carteira.Pontos} pontos, mas precisa de {recompensa.Pontos} pontos.", null);
            }

            var carteiraAtual = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteiraAtual == null)
            {
                return (false, "Carteira digital não encontrada", null);
            }

            // Deduzir os pontos
            carteiraAtual.Pontos -= recompensa.Pontos;
            await _supabaseClient.From<CarteiraDigital>().Update(carteiraAtual);

            // Gerar um ticket para rastrear o resgate
            var ticketDTO = new TicketCreateDTO
            {
                Token = token,
                TipoOperacao = "ResgateRecompensa",
                Descricao = $"Resgate da recompensa: {recompensa.Nome} (ID: {recompensa.Id})",
                Valor = 0.124 // Não envolve saldo, apenas pontos
            };

            var ticketResult = await _ticketService.CriarTicket(ticketDTO);
            if (!ticketResult.success)
            {
                carteiraAtual.Pontos += recompensa.Pontos;
                await _supabaseClient.From<CarteiraDigital>().Update(carteiraAtual);
                return (false, $"Falha ao gerar ticket para o resgate: {ticketResult.message}", null);
            }

            // Registrar o resgate com status "Pendente" e associar o TicketCode
            var recompensaUsuario = new RecompensaUsuario
            {
                RecompensaId = recompensaId,
                UsuarioId = userId,
                DataRecompensa = DateTime.UtcNow,
                Status = "Pendente",
                TicketCode = ticketResult.ticket != null ? ticketResult.ticket.TicketCode : string.Empty
            };

            var insertResponse = await _supabaseClient
                .From<RecompensaUsuario>()
                .Insert(recompensaUsuario);

            if (insertResponse == null || !insertResponse.Models.Any())
            {
                carteiraAtual.Pontos += recompensa.Pontos;
                await _supabaseClient.From<CarteiraDigital>().Update(carteiraAtual);
                if (ticketResult.ticket != null)
                {
                    await _supabaseClient.From<Ticket>()
                        .Where(t => t.Id == ticketResult.ticket.Id)
                        .Delete();
                }
                return (false, "Falha ao registrar a recompensa na conta do usuário", null);
            }

            // Enviar notificação ao usuário sobre o resgate solicitado
            var notificacaoSolicitada = $"Você solicitou o resgate da recompensa '{recompensa.Nome}'. Seu TicketCode é {ticketResult.ticket?.TicketCode ?? "N/A"}. Um administrador entrará em contato em breve.";
            await _notificacaoService.CriarNotificacaoPessoal(
                usuarioId: userId,
                mensagem: notificacaoSolicitada,
                tipo: "Resgate de Recompensa Solicitado",
                dataExpiracao: DateTime.UtcNow.AddDays(7)
            );

            // Preparar a resposta
            var resgateResponse = new RecompensaResgateResponseDTO
            {
                RecompensaId = recompensa.Id,
                Nome = recompensa.Nome,
                Tipo = recompensa.Tipo,
                Descricao = recompensa.Descricao,
                Pontos = recompensa.Pontos,
                DataResgate = DateTime.UtcNow,
                Status = "Pendente",
                TicketCode = ticketResult.ticket?.TicketCode ?? "N/A"
            };

            return (true, "Resgate solicitado com sucesso. Um administrador entrará em contato em breve.", resgateResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao resgatar recompensa: {ex.Message}");
            return (false, $"Erro ao resgatar recompensa: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Lista as recompensas resgatadas pelo usuário
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaUsuarioResponseDTO>? recompensas)> ListarRecompensasUsuario(
        string token,
        string? status = null)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            long userId = validationResult.userId;

            // Buscar os registros de recompensas do usuário
            var query = _supabaseClient
                .From<RecompensaUsuario>()
                .Where(ru => ru.UsuarioId == userId);

            var recompensasUsuario = await query.Get();

            if (recompensasUsuario == null || !recompensasUsuario.Models.Any())
            {
                return (true, "Nenhuma recompensa resgatada encontrada", new List<RecompensaUsuarioResponseDTO>());
            }

            // Buscar os detalhes de cada recompensa
            var recompensasDTO = new List<RecompensaUsuarioResponseDTO>();
            foreach (var ru in recompensasUsuario.Models)
            {
                var recompensa = await _supabaseClient
                    .From<Recompensa>()
                    .Where(r => r.Id == ru.RecompensaId)
                    .Single();

                if (recompensa != null)
                {
                    var dto = new RecompensaUsuarioResponseDTO
                    {
                        Id = recompensa.Id,
                        Nome = recompensa.Nome,
                        Tipo = recompensa.Tipo,
                        Descricao = recompensa.Descricao,
                        Pontos = recompensa.Pontos,
                        DataResgate = ru.DataRecompensa,
                        Status = "Resgatada" // Poderia ter lógica para diferenciar status
                    };

                    // Aplicar filtro por status, se fornecido
                    if (string.IsNullOrEmpty(status) || dto.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    {
                        recompensasDTO.Add(dto);
                    }
                }
            }

            return (true, "Recompensas do usuário listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas do usuário: {ex.Message}");
            return (false, $"Erro ao listar recompensas do usuário: {ex.Message}", null);
        }
    }
}