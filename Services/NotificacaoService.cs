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
            var dataAtual = DateTime.UtcNow;

            // Passo 1: Busca TODAS as notificações de uma vez, sem filtros.
            var response = await _supabaseService.GetClient().From<Notificacao>()
                .Select("*,notificacoes_lidas(*)")
                .Get();
            
            if (response.Models == null)
            {
                _logger.LogWarning("Nenhum modelo retornado da busca de notificações.");
                return (true, "Nenhuma notificação encontrada.", new List<NotificacaoResponseDTO>());
            }

            // Passo 2: Filtrar tudo em C#, onde temos controle total.
            var notificacoesRelevantes = response.Models
                .Where(n => n.UsuarioId == userId || n.UsuarioId == null)
                .ToList();

            var notificacoesAtivas = notificacoesRelevantes
                .Where(n => n.DataExpiracao == null || n.DataExpiracao > dataAtual)
                .ToList();
            
            IEnumerable<Notificacao> notificacoesFiltradas = notificacoesAtivas;

            if (!string.IsNullOrEmpty(lida) && bool.TryParse(lida, out bool isLida))
            {
                if (isLida)
                {
                    notificacoesFiltradas = notificacoesAtivas.Where(n => 
                        (n.UsuarioId.HasValue && n.Lidos > 0) || 
                        (!n.UsuarioId.HasValue && n.NotificacoesLidas.Any(nl => nl.UsuarioId == userId))
                    );
                }
                else
                {
                    notificacoesFiltradas = notificacoesAtivas.Where(n => 
                        (n.UsuarioId.HasValue && n.Lidos == 0) || 
                        (!n.UsuarioId.HasValue && !n.NotificacoesLidas.Any(nl => nl.UsuarioId == userId))
                    );
                }
            }

            var notificacoesOrdenadas = notificacoesFiltradas
                .OrderByDescending(n => n.UsuarioId.HasValue ? n.Lidos == 0 : !n.NotificacoesLidas.Any(nl => nl.UsuarioId == userId))
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
                Lidos = n.UsuarioId.HasValue ? (int)n.Lidos : (n.NotificacoesLidas.Any(nl => nl.UsuarioId == userId) ? 1 : 0),
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

            var notificacao = await _supabaseService.GetClient().From<Notificacao>().Filter("id", Operator.Equals, notificacaoId).Single();
            if (notificacao == null) return (false, "Notificação não encontrada");

            if (notificacao.UsuarioId.HasValue)
            {
                if (notificacao.UsuarioId != userId) return (false, "Notificação não pertence ao usuário");

                if (notificacao.Lidos == 0)
                {
                    await _supabaseService.GetClient().From<Notificacao>().Where(x => x.Id == notificacaoId).Set(x => x.Lidos, 1).Update();
                }
            }
            else
            {
                var leituraExistente = await _supabaseService.GetClient().From<NotificacaoLida>().Filter("usuario_id", Operator.Equals, userId).Filter("notificacao_id", Operator.Equals, notificacaoId).Get();
                if (!leituraExistente.Models.Any())
                {
                    var novaLeitura = new NotificacaoLida { UsuarioId = userId, NotificacaoId = notificacaoId, DataLeitura = DateTime.UtcNow };
                    await _supabaseService.GetClient().From<NotificacaoLida>().Insert(novaLeitura);
                }
            }
            return (true, "Notificação marcada como lida com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação como lida");
            return (false, "Erro ao marcar notificação como lida");
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
            if (!response.success)
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
            return (false, "Erro ao marcar todas as notificações como lidas");
        }
    }
}