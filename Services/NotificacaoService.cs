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

    // Método auxiliar para enviar emails
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
                            <p>&copy; 2025 EcoIpil. Todos os direitos reservados.</p>
                        </div>
                    </div>
                </body>
                </html>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
                var smtpPort = !string.IsNullOrEmpty(smtpPortStr) ? int.Parse(smtpPortStr) : 587; // Valor padrão se nulo
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
            throw; // Opcional: rethrow para depuração
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

            // Verificar preferências de notificação por email com robustez
            if (usuario.Preferencias != null)
            {
                _logger.LogInformation("Preferências encontradas para o usuário {UserId}: {Preferencias}", usuarioId, JsonSerializer.Serialize(usuario.Preferencias));

                // Converter para dicionário de forma segura
                var preferencias = usuario.Preferencias as Dictionary<string, bool>;
                if (preferencias == null)
                {
                    _logger.LogWarning("Preferências não são um Dictionary<string, bool> para o usuário {UserId}", usuarioId);
                    return (true, "Notificação criada, mas preferências inválidas");
                }

                // Verificar a chave "notificacoes_email"
                if (preferencias.TryGetValue("notificacoes_email", out bool emailEnabled))
                {
                    if (emailEnabled)
                    {
                        _logger.LogInformation("Enviando email para {Email}", usuario.Email);
                        await EnviarEmailNotificacao(usuario.Email, mensagem, tipo ?? "Notificação");
                        await Task.Delay(2000);
                    }
                    else
                    {
                        _logger.LogInformation("Notificação por email desativada para o usuário {UserId}", usuarioId);
                    }
                }
                else
                {
                    _logger.LogInformation("Chave 'notificacoes_email' não encontrada, assumindo false para o usuário {UserId}", usuarioId);
                }
            }
            else
            {
                _logger.LogInformation("Preferências de notificação não definidas (nulas) para o usuário {UserId}", usuarioId);
            }

            _logger.LogInformation("Notificação pessoal criada para o usuário {UserId} com mensagem: {Mensagem}", usuarioId, mensagem);
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

            // Enviar email para todos os usuários com "notificacoes_email": true
            var usuarios = await _supabaseService.GetClient().From<Usuario>().Select("*").Get();
            foreach (var usuario in usuarios.Models)
            {
                if (usuario.Preferencias != null)
                {
                    _logger.LogInformation("Preferências encontradas para o usuário {UserId}: {Preferencias}", usuario.Id, JsonSerializer.Serialize(usuario.Preferencias));

                    // Tentar converter as preferências para um dicionário genérico
                    IDictionary<string, object>? preferencias = null;
                    if (usuario.Preferencias is IDictionary<string, object> dictObj)
                    {
                        preferencias = dictObj;
                    }
                    else if (usuario.Preferencias is IDictionary<string, bool> dictBool)
                    {
                        preferencias = dictBool.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                    }
                    else
                    {
                        // Se não for um dicionário, tentar desserializar como JSON
                        try
                        {
                            var jsonString = JsonSerializer.Serialize(usuario.Preferencias);
                            preferencias = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao desserializar preferências para o usuário {UserId}: {Preferencias}", usuario.Id, usuario.Preferencias?.ToString());
                        }
                    }

                    if (preferencias != null && preferencias.TryGetValue("notificacoes_email", out var emailEnabledObj))
                    {
                        bool emailEnabled = false;
                        if (emailEnabledObj is bool boolValue)
                        {
                            emailEnabled = boolValue;
                        }
                        else if (emailEnabledObj is string stringValue)
                        {
                            emailEnabled = stringValue.ToLower() == "true";
                        }
                        else
                        {
                            _logger.LogWarning("Valor de notificacoes_email não é booleano nem string para o usuário {UserId}: {Value}", usuario.Id, emailEnabledObj?.ToString());
                        }

                        if (emailEnabled)
                        {
                            await EnviarEmailNotificacao(usuario.Email, mensagem, tipo ?? "Notificação Geral");
                            await Task.Delay(3000);
                        }
                    }
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

            var todasNotificacoes = new List<Notificacao>();
            var dataAtual = DateTime.UtcNow;
            var dataAtualIso = dataAtual.ToString("o"); // Formato ISO 8601 (ex.: 2025-03-27T12:34:56.789Z)
            _logger.LogInformation("Data atual usada para filtro: {DataAtual}", dataAtualIso);

            // 1. Buscar notificações gerais usando a função get_notificacoes_gerais
            var responseGerais = await _supabaseService.GetClient()
                .Rpc("get_notificacoes_gerais", new { p_data_atual = dataAtualIso });

            // Usar diretamente o Content, que já é uma string
            string? contentGerais = responseGerais.Content;

            // Desserializar a resposta bruta em uma lista de Notificacao
            var notificacoesGerais = string.IsNullOrEmpty(contentGerais)
                ? new List<Notificacao>()
                : JsonSerializer.Deserialize<List<Notificacao>>(contentGerais, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Notificacao>();

            if (!string.IsNullOrEmpty(lida))
            {
                if (bool.TryParse(lida, out bool isLida))
                {
                    if (isLida)
                    {
                        // Notificações gerais que o usuário já leu
                        var notificacoesLidas = await _supabaseService.GetClient().From<NotificacaoLida>()
                            .Filter("usuario_id", Operator.Equals, userId)
                            .Get();

                        if (notificacoesLidas.Models.Any())
                        {
                            var notificacaoIds = notificacoesLidas.Models.Select(nl => nl.NotificacaoId).ToList();
                            var notificacoesLidasFiltradas = notificacoesGerais
                                .Where(n => notificacaoIds.Contains(n.Id))
                                .ToList();
                            todasNotificacoes.AddRange(notificacoesLidasFiltradas);
                        }
                    }
                    else
                    {
                        // Notificações gerais que o usuário ainda não leu
                        var notificacoesLidas = await _supabaseService.GetClient().From<NotificacaoLida>()
                            .Filter("usuario_id", Operator.Equals, userId)
                            .Get();

                        if (notificacoesLidas.Models.Any())
                        {
                            var notificacaoIds = notificacoesLidas.Models.Select(nl => nl.NotificacaoId).ToList();
                            var notificacoesNaoLidas = notificacoesGerais
                                .Where(n => !notificacaoIds.Contains(n.Id))
                                .ToList();
                            todasNotificacoes.AddRange(notificacoesNaoLidas);
                        }
                        else
                        {
                            todasNotificacoes.AddRange(notificacoesGerais);
                        }
                    }
                }
                else
                {
                    return (false, "Parâmetro 'lida' deve ser 'true' ou 'false'", new List<NotificacaoResponseDTO>());
                }
            }
            else
            {
                todasNotificacoes.AddRange(notificacoesGerais);
            }

            // 2. Buscar notificações pessoais usando a função get_notificacoes_pessoais
            bool? lidaParam = string.IsNullOrEmpty(lida) ? null : bool.Parse(lida);
            var responsePessoais = await _supabaseService.GetClient()
                .Rpc("get_notificacoes_pessoais", new { p_usuario_id = userId, p_data_atual = dataAtualIso, p_lida = lidaParam });

            // Usar diretamente o Content, que já é uma string
            string? contentPessoais = responsePessoais.Content;

            // Desserializar a resposta bruta em uma lista de Notificacao
            var notificacoesPessoais = string.IsNullOrEmpty(contentPessoais)
                ? new List<Notificacao>()
                : JsonSerializer.Deserialize<List<Notificacao>>(contentPessoais, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Notificacao>();

            todasNotificacoes.AddRange(notificacoesPessoais);

            // 3. Ordenar por created_at (mais recente primeiro) e aplicar paginação
            todasNotificacoes = todasNotificacoes.OrderByDescending(n => n.CreatedAt).ToList();

            if (pagina.HasValue && limite.HasValue)
            {
                int from = (pagina.Value - 1) * limite.Value;
                int to = from + limite.Value;
                todasNotificacoes = todasNotificacoes.Skip(from).Take(limite.Value).ToList();
            }

            // 4. Mapear para DTO
            var notificacoesDTO = todasNotificacoes.Select(n => new NotificacaoResponseDTO
            {
                Id = n.Id,
                Mensagem = n.Mensagem,
                Tipo = n.Tipo,
                Lidos = (int)n.Lidos,
                DataExpiracao = n.DataExpiracao
            }).ToList();

            return (true, "Notificações obtidas com sucesso", notificacoesDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar notificações");
            return (false, "Erro ao listar notificações", new List<NotificacaoResponseDTO>());
        }
    }

    public async Task<(bool success, string message)> MarcarComoLida(string token, long notificacaoId)
    {
        try
        {
            var (success, message, validatedUserId) = await ValidateToken(token);
            if (!success || !validatedUserId.HasValue)
            {
                return (false, message);
            }
            
            long userId = validatedUserId.Value;

            var notificacao = await _supabaseService.GetClient().From<Notificacao>()
                .Filter("id", Operator.Equals, notificacaoId)
                .Single();

            if (notificacao == null)
            {
                return (false, "Notificação não encontrada");
            }

            if (notificacao.UsuarioId.HasValue)
            {
                if (notificacao.UsuarioId != userId)
                {
                    return (false, "Notificação não pertence ao usuário");
                }

                notificacao.Lidos++;
                await _supabaseService.GetClient().From<Notificacao>()
                    .Where(x => x.Id == notificacaoId)
                    .Set(x => x.Lidos, notificacao.Lidos)
                    .Update();

                return (true, "Notificação marcada como lida com sucesso");
            }
            else
            {
                var leituraExistente = await _supabaseService.GetClient().From<NotificacaoLida>()
                    .Filter("usuario_id", Operator.Equals, userId)
                    .Filter("notificacao_id", Operator.Equals, notificacaoId)
                    .Single();

                if (leituraExistente != null)
                {
                    return (false, "Você já marcou esta notificação como lida");
                }

                var novaLeitura = new NotificacaoLida
                {
                    UsuarioId = userId,
                    NotificacaoId = notificacaoId,
                    DataLeitura = DateTime.UtcNow
                };
                await _supabaseService.GetClient().From<NotificacaoLida>().Insert(novaLeitura);

                notificacao.Lidos++;
                await _supabaseService.GetClient().From<Notificacao>()
                    .Where(x => x.Id == notificacaoId)
                    .Set(x => x.Lidos, notificacao.Lidos)
                    .Update();

                return (true, "Notificação geral marcada como lida com sucesso");
            }
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
            if (!success || !validatedUserId.HasValue)
            {
                return (false, message);
            }
            
            long userId = validatedUserId.Value;

            var notificacoes = await _supabaseService.GetClient().From<Notificacao>()
                .Filter("usuario_id", Operator.Equals, userId)
                .Get();

            if (notificacoes.Models == null || !notificacoes.Models.Any())
            {
                return (true, "Nenhuma notificação para marcar como lida");
            }

            foreach (var notificacao in notificacoes.Models)
            {
                if (notificacao.Lidos == 0)
                {
                    await _supabaseService.GetClient().From<Notificacao>()
                        .Where(x => x.Id == notificacao.Id)
                        .Set(x => x.Lidos, 1)
                        .Update();
                }
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