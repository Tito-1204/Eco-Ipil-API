using Supabase;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using EcoIpil.API.Models;
using EcoIpil.API.DTOs;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net.Http;
using Newtonsoft.Json; // Para serializar o payload da requisição

namespace EcoIpil.API.Services
{
    public class ConfiguracaoService
    {
        private readonly Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public ConfiguracaoService(SupabaseService supabaseService, IConfiguration configuration)
        {
            _supabaseClient = supabaseService.GetClient();
            _configuration = configuration;
        }

        public async Task<(bool success, string message, object? data)> ObterConfiguracoes(long userId)
        {
            try
            {
                var usuario = await _supabaseClient
                    .From<Usuario>()
                    .Where(u => u.Id == userId)
                    .Select("nome, telefone, email, localizacao, preferencias")
                    .Single();

                if (usuario == null)
                    return (false, "Usuário não encontrado", null);

                return (true, "Configurações obtidas com sucesso", new
                {
                    usuario.Nome,
                    usuario.Telefone,
                    usuario.Email,
                    usuario.Localizacao,
                    usuario.Preferencias
                });
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao obter configurações: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> AtualizarConfiguracoes(long userId, AtualizarConfiguracaoRequestDTO request)
        {
            try
            {
                var usuario = await _supabaseClient
                    .From<Usuario>()
                    .Where(u => u.Id == userId)
                    .Single();

                if (usuario == null)
                    return (false, "Usuário não encontrado");

                bool personalDataUpdate = false;

                // Check for personal data updates
                if (!string.IsNullOrEmpty(request.Nome) || 
                    !string.IsNullOrEmpty(request.Localizacao) || 
                    !string.IsNullOrEmpty(request.Telefone) || 
                    !string.IsNullOrEmpty(request.Email))
                {
                    if (usuario.LastPersonalDataUpdate != null && (DateTime.UtcNow - usuario.LastPersonalDataUpdate.Value).TotalDays < 30)
                    {
                        return (false, "Você só pode atualizar dados pessoais uma vez por mês.");
                    }
                    personalDataUpdate = true;
                }

                // Update name
                if (!string.IsNullOrEmpty(request.Nome))
                {
                    if (string.IsNullOrEmpty(request.Senha) || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                        return (false, "Senha incorreta");
                    usuario.Nome = request.Nome;
                }

                // Update location
                if (!string.IsNullOrEmpty(request.Localizacao))
                {
                    if (string.IsNullOrEmpty(request.Senha) || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                        return (false, "Senha incorreta");
                    usuario.Localizacao = request.Localizacao;
                }

                // Update phone
                if (!string.IsNullOrEmpty(request.Telefone))
                {
                    if (string.IsNullOrEmpty(request.Senha) || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                        return (false, "Senha incorreta");
                    var novoTelefone = request.Telefone;
                    var codigo = new Random().Next(100000, 999999).ToString();
                    await _supabaseClient.From<Verificacao>()
                        .Insert(new Verificacao 
                        { 
                            UserId = userId, 
                            Codigo = codigo, 
                            Telefone = novoTelefone,
                            ExpiresAt = DateTime.UtcNow.AddHours(24)
                        });
                    await SendConfirmationEmail(usuario.Email, codigo);
                    // Do not update phone yet
                }

                // Update email
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var novoEmail = request.Email;
                    var token = Guid.NewGuid().ToString();
                    await _supabaseClient.From<Verificacao>()
                        .Insert(new Verificacao 
                        { 
                            UserId = userId, 
                            Token = token, 
                            Email = novoEmail,
                            ExpiresAt = DateTime.UtcNow.AddHours(24)
                        });
                    Console.WriteLine($"Token de verificação gerado: {token} para o email {novoEmail}");
                    await SendEmailConfirmation(novoEmail, token);
                }

                // Atualização das preferências com chaves em minúsculas
                if (request.Preferencias != null)
                {
                    var preferenciasLowercase = new Dictionary<string, bool>();
                    if (request.Preferencias.NotificacoesApp.HasValue)
                        preferenciasLowercase["notificacoes_app"] = request.Preferencias.NotificacoesApp.Value;
                    if (request.Preferencias.NotificacoesEmail.HasValue)
                        preferenciasLowercase["notificacoes_email"] = request.Preferencias.NotificacoesEmail.Value;
                    usuario.Preferencias = preferenciasLowercase;
                }

                // Update user, but only if not waiting for confirmation
                if (string.IsNullOrEmpty(request.Telefone) && string.IsNullOrEmpty(request.Email))
                {
                    if (personalDataUpdate)
                    {
                        usuario.LastPersonalDataUpdate = DateTime.UtcNow;
                    }
                    await _supabaseClient.From<Usuario>()
                        .Where(u => u.Id == userId)
                        .Update(usuario);
                }

                return (true, "Configurações atualizadas com sucesso. Verifique seu email/telefone para confirmação, se aplicável.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao atualizar configurações: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ConfirmarAlteracaoTelefone(long userId, string codigo)
        {
            try
            {
                var verificacao = await _supabaseClient.From<Verificacao>()
                    .Where(v => v.UserId == userId && v.Codigo == codigo)
                    .Single();

                if (verificacao == null)
                    return (false, "Código inválido");

                if (verificacao.ExpiresAt != null && verificacao.ExpiresAt < DateTime.UtcNow)
                {
                    await _supabaseClient.From<Verificacao>()
                        .Where(v => v.UserId == userId)
                        .Delete();
                    return (false, "Código expirado");
                }

                if (string.IsNullOrEmpty(verificacao.Telefone))
                {
                    return (false, "O telefone informado é inválido");
                }

                var usuario = await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == userId)
                    .Single();

                if (usuario == null)
                    return (false, "Usuário não encontrado");

                usuario.Telefone = verificacao.Telefone;
                usuario.LastPersonalDataUpdate = DateTime.UtcNow;
                await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == userId)
                    .Update(usuario);

                await _supabaseClient.From<Verificacao>()
                    .Where(v => v.UserId == userId)
                    .Delete();

                return (true, "Telefone atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao confirmar alteração de telefone: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ConfirmarEmail(string token)
        {
            try
            {
                // 1. Buscar a verificação pelo token
                var verificacao = await _supabaseClient.From<Verificacao>()
                    .Where(v => v.Token == token)
                    .Single();

                if (verificacao == null)
                    return (false, "Token inválido ou expirado");

                if (verificacao.ExpiresAt != null && verificacao.ExpiresAt < DateTime.UtcNow)
                {
                    await _supabaseClient.From<Verificacao>()
                        .Where(v => v.Token == token)
                        .Delete();
                    return (false, "Token expirado");
                }

                // 2. Buscar o usuário pelo UserId da verificação
                var usuario = await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == verificacao.UserId)
                    .Single();

                if (usuario == null)
                    return (false, "Usuário não encontrado");

                // 3. Atualizar o email na tabela auth.users via API REST
                var novoEmail = verificacao.Email;
                if (string.IsNullOrEmpty(novoEmail))
                {
                    return (false, "Email de verificação inválido");
                }
                var userUid = usuario.UserUid;

                var supabaseUrl = _configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL não configurada");
                var supabaseServiceKey = _configuration["Supabase:ServiceKey"] ?? throw new InvalidOperationException("Supabase Service Key não configurada");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceKey}");

                    var updatePayload = new
                    {
                        email = novoEmail
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(updatePayload), System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PutAsync($"{supabaseUrl}/auth/v1/admin/users/{userUid}", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        return (false, $"Erro ao atualizar email na autenticação: {errorMessage}");
                    }
                }

                // 4. Atualizar o email na tabela usuarios
                usuario.Email = novoEmail;
                usuario.LastPersonalDataUpdate = DateTime.UtcNow;
                await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == verificacao.UserId)
                    .Update(usuario);

                // 5. Deletar a verificação após a confirmação
                await _supabaseClient.From<Verificacao>()
                    .Where(v => v.Token == token)
                    .Delete();

                return (true, "Email atualizado com sucesso. Um email de confirmação foi enviado para o novo endereço.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao confirmar email: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, string uid)> ObterUid(long userId)
        {
            try
            {
                var usuario = await _supabaseClient
                    .From<Usuario>()
                    .Where(u => u.Id == userId)
                    .Select("user_uid")
                    .Single();

                if (usuario == null)
                    return (false, "Usuário não encontrado", string.Empty);

                return (true, "UID obtido com sucesso", usuario.UserUid);
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao obter UID: {ex.Message}", string.Empty);
            }
        }

        private async Task SendConfirmationEmail(string email, string code)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("EcoIpil", _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail não configurado")));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Confirmação de Alteração de Telefone";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Confirmação de Alteração de Telefone - EcoIpil</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden; }}
                            .header {{ background-color: #28a745; color: #ffffff; text-align: center; padding: 20px; }}
                            .header h1 {{ margin: 0; font-size: 24px; }}
                            .content {{ padding: 30px; text-align: center; color: #333333; }}
                            .content h2 {{ font-size: 20px; margin-bottom: 20px; }}
                            .code-box {{ background-color: #e8f5e9; border: 2px dashed #28a745; padding: 15px; font-size: 24px; font-weight: bold; color: #28a745; margin: 20px 0; letter-spacing: 2px; }}
                            .content p {{ font-size: 16px; line-height: 1.5; margin: 10px 0; }}
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
                                <h2>Confirmação de Alteração de Telefone</h2>
                                <p>Olá! Recebemos uma solicitação para alterar o número de telefone da sua conta EcoIpil.</p>
                                <p>Para confirmar a alteração, use o código abaixo no aplicativo:</p>
                                <div class=""code-box"">{code}</div>
                                <p>Este código é válido por 24 horas. Se você não solicitou essa alteração, ignore este email ou entre em contato com nosso suporte.</p>
                            </div>
                            <div class=""footer"">
                                <p>Precisa de ajuda? <a href=""mailto:suporte@eco-ipil.com"">Entre em contato com o suporte</a></p>
                                <p>&copy; 2023 EcoIpil. Todos os direitos reservados.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? throw new InvalidOperationException("SmtpServer não configurado");
                    var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? throw new InvalidOperationException("SmtpPort não configurado");
                    if (!int.TryParse(smtpPortStr, out int smtpPort))
                        throw new InvalidOperationException("SmtpPort inválido");

                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail não configurado"),
                        _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword não configurado")
                    );
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    Console.WriteLine($"Email de confirmação enviado para {email} com código {code}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de confirmação: {ex.Message}");
                throw;
            }
        }

        private async Task SendEmailConfirmation(string email, string token)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("EcoIpil", _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail não configurado")));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Confirmação de Alteração de Email";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Confirmação de Alteração de Email - EcoIpil</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden; }}
                            .header {{ background-color: #28a745; color: #ffffff; text-align: center; padding: 20px; }}
                            .header h1 {{ margin: 0; font-size: 24px; }}
                            .content {{ padding: 30px; text-align: center; color: #333333; }}
                            .content h2 {{ font-size: 20px; margin-bottom: 20px; }}
                            .token-box {{ background-color: #e8f5e9; border: 2px dashed #28a745; padding: 15px; font-size: 24px; font-weight: bold; color: #28a745; margin: 20px 0; letter-spacing: 2px; }}
                            .content p {{ font-size: 16px; line-height: 1.5; margin: 10px 0; }}
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
                                <h2>Confirmação de Alteração de Email</h2>
                                <p>Olá! Recebemos uma solicitação para alterar o email da sua conta EcoIpil.</p>
                                <p>Para confirmar a alteração, use o token abaixo no aplicativo:</p>
                                <div class=""token-box"">{token}</div>
                                <p>Este token é válido por 24 horas. Se você não solicitou essa alteração, ignore este email ou entre em contato com nosso suporte.</p>
                            </div>
                            <div class=""footer"">
                                <p>Precisa de ajuda? <a href=""mailto:suporte@eco-ipil.com"">Entre em contato com o suporte</a></p>
                                <p>&copy; 2023 EcoIpil. Todos os direitos reservados.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? throw new InvalidOperationException("SmtpServer não configurado");
                    var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? throw new InvalidOperationException("SmtpPort não configurado");
                    if (!int.TryParse(smtpPortStr, out int smtpPort))
                        throw new InvalidOperationException("SmtpPort inválido");

                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail não configurado"),
                        _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword não configurado")
                    );
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    Console.WriteLine($"Email de confirmação enviado para {email} com token {token}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de confirmação: {ex.Message}");
                throw;
            }
        }
    }
}