using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using EcoIpil.API.DTOs;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;
using System.Text.Json;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using System.Configuration;

namespace EcoIpil.API.Services;

public class NotificacaoService
{
    private readonly SupabaseService _supabaseService;
    private readonly UsuarioService _usuarioService;
    private readonly AtividadeService _atividadeService;
    private readonly ILogger<NotificacaoService> _logger;
    private readonly IConfiguration _configuration;

    public NotificacaoService(SupabaseService supabaseService, UsuarioService usuarioService, AtividadeService atividadeService, ILogger<NotificacaoService> logger, IConfiguration configuration)
    {
        _supabaseService = supabaseService;
        _usuarioService = usuarioService;
        _atividadeService = atividadeService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task EnviarEmailNotificacao(string email, string mensagem, string tipo)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("EcoIpil", _configuration["EmailSettings:SenderEmail"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = $"Notificação EcoIpil: {tipo}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <!DOCTYPE html>
                <html lang=""pt-BR"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Notificação EcoIpil: {tipo}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden; }}
                        .header {{ background-color: #28a745; color: #ffffff; text-align: center; padding: 20px; }}
                        .header h1 {{ margin: 0; font-size: 24px; }}
                        .content {{ padding: 30px; text-align: center; color: #333333; }}
                        .content h2 {{ font-size: 20px; margin-bottom: 20px; color: #28a745; }}
                        .message-box {{ background-color: #e8f5e9; border: 2px solid #28a745; padding: 20px; font-size: 16px; color: #333333; margin: 20px 0; border-radius: 8px; }}
                        .content p {{ font-size: 16px; line-height: 1.5; margin: 10px 0; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #28a745; color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; margin-top: 20px; }}
                        .button:hover {{ background-color: #218838; }}
                        .footer {{ background-color: #f4f4f4; text-align: center; padding: 15px; font-size: 14px; color: #666666; }}
                        .footer a {{ color: #28a745; text-decoration: none; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>EcoIpil</h1>
                        </div>
                        <div class=""content"">
                            <h2>{tipo}</h2>
                            <p>Olá! Temos uma nova notificação para você:</p>
                            <div class=""message-box"">{mensagem}</div>
                            <p>Não perca esta oportunidade de se engajar com a EcoIpil e fazer a diferença!</p>
                            <a href=""https://eco-ipil.com/notificacoes"" class=""button"">Ver Notificações</a>
                        </div>
                        <div class=""footer"">
                            <p>Precisa de ajuda? <a href=""mailto:suporte@eco-ipil.com"">Entre em contato com o suporte</a></p>
                            <p>© 2025 EcoIpil. Todos os direitos reservados.</p>
                        </div>
                    </div>
                </body>
                </html>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
                var smtpPort = !string.IsNullOrEmpty(smtpPortStr) ? int.Parse(smtpPortStr) : 587;
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];

                _logger.LogInformation("Tentando conectar ao servidor SMTP {SmtpServer}:{SmtpPort}", smtpServer, smtpPort);
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                _logger.LogInformation("Conectado ao servidor SMTP com sucesso");

                _logger.LogInformation("Tentando autenticar com {SenderEmail}", senderEmail);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                _logger.LogInformation("Autenticado com sucesso");

                _logger.LogInformation("Enviando email para {Email}", email);
                await client.SendAsync(message);
                _logger.LogInformation("Email enviado com sucesso para {Email}", email);

                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email para {Email}: {Message}", email, ex.Message);
            throw;
        }
    }

    public async Task<(bool success, string message)> CriarNotificacaoPessoal(long usuarioId, string mensagem, string? tipo = null, DateTime? dataExpiracao = null)
    {
        try
        {
            var usuario = await _supabaseService.GetClient().From<Usuario>().Where(u => u.Id == usuarioId).Single();
            if (usuario == null) return (false, "Usuário não encontrado");

            var notificacao = new Notificacao
            {
                CreatedAt = DateTime.UtcNow,
                Mensagem = mensagem,
                Tipo = tipo,
                Lidos = 0,
                DataExpiracao = dataExpiracao,
                UsuarioId = usuarioId
            };

            await _supabaseService.GetClient().From<Notificacao>().Insert(notificacao);

            if (usuario.Preferencias != null)
            {
                var preferencias = usuario.Preferencias as Dictionary<string, bool>;
                if (preferencias != null && preferencias.TryGetValue("notificacoes_email", out bool emailEnabled) && emailEnabled)
                {
                    await EnviarEmailNotificacao(usuario.Email, mensagem, tipo ?? "Notificação");
                    await Task.Delay(2000);
                }
            }

            _logger.LogInformation("Notificação pessoal criada para o usuário {UserId}", usuarioId);
            return (true, "Notificação pessoal criada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação pessoal para o usuário {UserId}", usuarioId);
            return (false, "Erro ao criar notificação pessoal");
        }
    }

    public async Task<(bool success, string message)> CriarNotificacaoGeral(string mensagem, string? tipo = null, DateTime? dataExpiracao = null)
    {
        try
        {
            var notificacao = new Notificacao
            {
                CreatedAt = DateTime.UtcNow,
                Mensagem = mensagem,
                Tipo = tipo,
                Lidos = 0,
                DataExpiracao = dataExpiracao,
                UsuarioId = null
            };

            await _supabaseService.GetClient().From<Notificacao>().Insert(notificacao);

            var usuarios = await _supabaseService.GetClient().From<Usuario>().Select("*").Get();
            foreach (var usuario in usuarios.Models)
            {
                if (usuario.Preferencias is IDictionary<string, object> preferencias &&
                    preferencias.TryGetValue("notificacoes_email", out var emailEnabledObj) &&
                    emailEnabledObj is bool emailEnabled && emailEnabled)
                {
                    await EnviarEmailNotificacao(usuario.Email, mensagem, tipo ?? "Notificação Geral");
                    await Task.Delay(3000);
                }
            }

            _logger.LogInformation("Notificação geral criada com mensagem: {Mensagem}", mensagem);
            return (true, "Notificação geral criada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação geral");
            return (false, "Erro ao criar notificação geral");
        }
    }

    private async Task<(bool success, string message, long? userId)> ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, "Token não pode ser nulo ou vazio", null);
        }

        var validationResult = await _usuarioService.ValidateToken(token);
        if (!validationResult.success)
        {
            return (false, validationResult.message, null);
        }
        return (true, "Token válido", validationResult.userId);
    }

    public async Task<(bool success, string message, List<NotificacaoResponseDTO> notificacoes)> ListarNotificacoes(string token, string? lida, int? pagina, int? limite)
    {
        try
        {
            var (success, message, validatedUserId) = await ValidateToken(token);
            if (!success || !validatedUserId.HasValue)
            {
                return (false, message, new List<NotificacaoResponseDTO>());
            }
            
            long userId = validatedUserId.Value;
            var dataAtualIso = DateTime.UtcNow.ToString("o");
            
            // Buscar todas as marcações de leitura efetuadas pelo usuário na tabela notificacoes_lidas
            // Usar cliente admin para garantir que não é bloqueado por RLS
            var lidasResponse = await _supabaseService.GetAdminClient().From<NotificacaoLida>()
                .Where(nl => nl.UsuarioId == userId)
                .Get();
            var notificacoesLidasIds = lidasResponse.Models?.Select(nl => nl.NotificacaoId).ToHashSet() ?? new HashSet<long>();
            _logger.LogInformation("Utilizador {UserId} tem {Count} notificações lidas registadas", userId, notificacoesLidasIds.Count);
            
            var todasNotificacoes = new List<Notificacao>();
            
            // Chamar RPC para notificações gerais (agentes/geral)
            var responseGerais = await _supabaseService.GetClient().Rpc("get_notificacoes_gerais", new { p_data_atual = dataAtualIso });
            if (responseGerais.ResponseMessage.IsSuccessStatusCode && !string.IsNullOrEmpty(responseGerais.Content))
            {
                var notificacoesGerais = JsonSerializer.Deserialize<List<Notificacao>>(responseGerais.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Notificacao>();
                todasNotificacoes.AddRange(notificacoesGerais);
            }
            
            // Chamar RPC para notificações pessoais
            bool? lidaParam = string.IsNullOrEmpty(lida) ? null : bool.Parse(lida);
            var responsePessoais = await _supabaseService.GetClient().Rpc("get_notificacoes_pessoais", new { p_usuario_id = userId, p_data_atual = dataAtualIso, p_lida = lidaParam });
            if (responsePessoais.ResponseMessage.IsSuccessStatusCode && !string.IsNullOrEmpty(responsePessoais.Content))
            {
                var notificacoesPessoais = JsonSerializer.Deserialize<List<Notificacao>>(responsePessoais.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Notificacao>();
                todasNotificacoes.AddRange(notificacoesPessoais);
            }

            // Remover duplicados se houver
            var notificacoesUnicas = todasNotificacoes.GroupBy(n => n.Id).Select(g => g.First()).ToList();

            // Filtrar status de leitura de forma unificada
            IEnumerable<Notificacao> notificacoesFiltradas;
            if (!string.IsNullOrEmpty(lida) && bool.TryParse(lida, out bool isLidaReq))
            {
                notificacoesFiltradas = notificacoesUnicas.Where(n => {
                    bool isLida = n.Lidos > 0 || notificacoesLidasIds.Contains(n.Id);
                    return isLidaReq ? isLida : !isLida;
                });
            }
            else 
            {
                notificacoesFiltradas = notificacoesUnicas;
            }

            var notificacoesOrdenadas = notificacoesFiltradas
                .OrderByDescending(n => (n.Lidos == 0 && !notificacoesLidasIds.Contains(n.Id)))
                .ThenByDescending(n => n.CreatedAt)
                .ToList();
            
            if (pagina.HasValue && limite.HasValue)
            {
                notificacoesOrdenadas = notificacoesOrdenadas.Skip((pagina.Value - 1) * limite.Value).Take(limite.Value).ToList();
            }

            var notificacoesDTO = notificacoesOrdenadas.Select(n => new NotificacaoResponseDTO
            {
                Id = n.Id,
                Mensagem = n.Mensagem,
                Tipo = n.Tipo,
                Lidos = (n.Lidos > 0 || notificacoesLidasIds.Contains(n.Id)) ? 1 : 0,
                DataExpiracao = n.DataExpiracao
            }).ToList();

            return (true, "Notificações obtidas com sucesso", notificacoesDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar notificações");
            return (false, "Erro interno ao listar notificações", new List<NotificacaoResponseDTO>());
        }
    }

    public async Task<(bool success, string message)> MarcarComoLida(string token, long notificacaoId)
    {
        try
        {
            var (success, message, validatedUserId) = await ValidateToken(token);
            if (!success || !validatedUserId.HasValue) return (false, message);
            
            long userId = validatedUserId.Value;

            // Usar cliente admin para leituras também (mais confiável)
            var response = await _supabaseService.GetAdminClient().From<Notificacao>()
                .Where(x => x.Id == notificacaoId)
                .Get();

            var notificacao = response.Models?.FirstOrDefault();
            if (notificacao == null)
            {
                return (false, "Notificação não encontrada");
            }

            // 1. Gravar registro permanente na tabela notificacoes_lidas
            //    Usar GetAdminClient() para contornar RLS do Supabase
            var leituraExistente = await _supabaseService.GetAdminClient().From<NotificacaoLida>()
                .Where(nl => nl.UsuarioId == userId && nl.NotificacaoId == notificacaoId)
                .Get();

            if (leituraExistente.Models == null || !leituraExistente.Models.Any())
            {
                var novaLeitura = new NotificacaoLida
                {
                    UsuarioId = userId,
                    NotificacaoId = notificacaoId,
                    DataLeitura = DateTime.UtcNow
                };
                await _supabaseService.GetAdminClient().From<NotificacaoLida>().Insert(novaLeitura);
                _logger.LogInformation("Leitura registrada: usuário {UserId}, notificação {NotificacaoId}", userId, notificacaoId);
            }
            else
            {
                _logger.LogInformation("Leitura já existente: usuário {UserId}, notificação {NotificacaoId}", userId, notificacaoId);
            }

            // 2. Se for notificação pessoal do usuário, atualizar também lidos na tabela notificacoes
            if (notificacao.UsuarioId.HasValue && notificacao.UsuarioId.Value == userId)
            {
                if (notificacao.Lidos == 0)
                {
                    notificacao.Lidos = 1;
                    await _supabaseService.GetAdminClient().From<Notificacao>()
                        .Where(x => x.Id == notificacaoId)
                        .Update(notificacao);
                    _logger.LogInformation("Campo lidos atualizado na tabela notificacoes: {NotificacaoId}", notificacaoId);
                }
            }

            return (true, "Notificação marcada como lida com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação {NotificacaoId} como lida", notificacaoId);
            return (false, $"Erro ao marcar notificação como lida: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> MarcarTodasComoLidas(string token)
    {
        try
        {
            var (success, message, validatedUserId) = await ValidateToken(token);
            if (!success || !validatedUserId.HasValue) return (false, message);
            
            long userId = validatedUserId.Value;

            var response = await ListarNotificacoes(token, "false", 1, 1000);
            if (!response.success || response.notificacoes == null)
            {
                 return (false, "Erro ao buscar notificações para marcar como lidas.");
            }

            foreach (var notificacaoDto in response.notificacoes)
            {
                await MarcarComoLida(token, notificacaoDto.Id);
            }

            return (true, "Todas as notificações marcadas como lidas com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações como lidas");
            return (false, $"Erro ao marcar todas como lidas: {ex.Message}");
        }
    }
}