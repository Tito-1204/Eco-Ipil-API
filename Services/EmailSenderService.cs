using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EcoIpil.API.Services
{
    public class EmailSenderService
    {
        private readonly IConfiguration _configuration;

        public EmailSenderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(bool success, string message)> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var sendgridKey = _configuration["EmailSettings:SendGridApiKey"];
            if (!string.IsNullOrEmpty(sendgridKey))
            {
                return await SendViaSendGridAsync(sendgridKey, toEmail, subject, htmlBody);
            }

            return await SendViaSmtpAsync(toEmail, subject, htmlBody);
        }

        private async Task<(bool success, string message)> SendViaSendGridAsync(string apiKey, string toEmail, string subject, string htmlBody)
        {
            try
            {
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                var senderName = _configuration["EmailSettings:SenderName"] ?? "ECO";

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(senderEmail, senderName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

                Console.WriteLine($"SendGrid: Enviando email para {toEmail}...");
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"SendGrid: Email enviado para {toEmail}");
                    return (true, "Email enviado com sucesso");
                }

                var errorBody = await response.Body.ReadAsStringAsync();
                Console.WriteLine($"SendGrid: Erro HTTP {(int)response.StatusCode}: {errorBody}");
                return (false, $"SendGrid error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendGrid: Exceção: {ex.GetType().Name}: {ex.Message}");
                return (false, $"SendGrid error: {ex.Message}");
            }
        }

        private async Task<(bool success, string message)> SendViaSmtpAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                var senderPassword = (_configuration["EmailSettings:SenderPassword"] ?? "").Replace(" ", "");
                var senderName = _configuration["EmailSettings:SenderName"] ?? "ECO";
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
                var smtpPort = !string.IsNullOrEmpty(smtpPortStr) ? int.Parse(smtpPortStr) : 587;
                var useSsl = smtpPort == 465;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 10000;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                var options = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                Console.WriteLine($"SMTP: Conectando a {smtpServer}:{smtpPort}...");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.ConnectAsync(smtpServer, smtpPort, options, cts.Token);
                Console.WriteLine("SMTP: Conectado. Autenticando...");

                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.AuthenticateAsync(senderEmail, senderPassword, cts2.Token);
                Console.WriteLine("SMTP: Autenticado. Enviando...");

                using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.SendAsync(message, cts3.Token);
                Console.WriteLine("SMTP: Email enviado. Desconectando...");

                await client.DisconnectAsync(true);
                Console.WriteLine($"SMTP: Email enviado para {toEmail}");
                return (true, "Email enviado com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP: ERRO: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"SMTP: InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                return (false, $"SMTP error: {ex.Message}");
            }
        }
    }
}
