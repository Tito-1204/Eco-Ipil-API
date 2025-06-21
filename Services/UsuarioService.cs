using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EcoIpil.API.Models;
using EcoIpil.API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants; // Adicionando para usar Operator
using System.IO;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using BCrypt.Net;

namespace EcoIpil.API.Services;

public class UsuarioService
{
    private readonly Supabase.Client _supabaseClient; // Cliente para operações gerais
    private readonly Supabase.Client _supabaseAdminClient; // Cliente para operações administrativas (Storage)
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private const string BUCKET_FOTOS_PERFIL = "fotos";

    public UsuarioService(SupabaseService supabaseService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _supabaseClient = supabaseService.GetClient();
        _supabaseAdminClient = supabaseService.GetAdminClient(); // Cliente admin para Storage
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        Task.Run(InicializarBucketFotosPerfil).Wait(); // Inicializa o bucket ao iniciar o serviço
    }
    
    private async Task InicializarBucketFotosPerfil()
    {
        try
        {
            // Verificar se o bucket existe usando o cliente admin
            var buckets = await _supabaseAdminClient.Storage.ListBuckets();
            var bucketExiste = buckets != null && buckets.Any(b => b.Name == BUCKET_FOTOS_PERFIL);
            
            if (!bucketExiste)
            {
                // Criar o bucket se não existir usando o cliente admin
                await _supabaseAdminClient.Storage.CreateBucket(BUCKET_FOTOS_PERFIL, new Supabase.Storage.BucketUpsertOptions
                {
                    Public = true // Tornar o bucket público para que as imagens possam ser acessadas diretamente
                });
                
                Console.WriteLine($"Bucket '{BUCKET_FOTOS_PERFIL}' criado com sucesso");
            }
            else
            {
                Console.WriteLine($"Bucket '{BUCKET_FOTOS_PERFIL}' já existe");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar bucket de fotos: {ex.Message}");
        }
    }

    public async Task<(bool success, string message, long userId)> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada")));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key,
                RequireExpirationTime = false
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userUid = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                         principal.FindFirst("uid")?.Value;

            Console.WriteLine($"UserUid encontrado no token: {userUid}");

            if (string.IsNullOrEmpty(userUid))
            {
                return (false, "Token inválido: UserUid não encontrado", 0);
            }

            var response = await _supabaseClient
                .From<Usuario>()
                .Select("id, user_uid")
                .Where(u => u.UserUid == userUid)
                .Get();

            var usuario = response.Models.FirstOrDefault();
            Console.WriteLine($"Usuário encontrado: {(usuario != null ? "Sim" : "Não")}");

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", 0);
            }

            return (true, "Token válido", usuario.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao validar token: {ex.Message}");
            return (false, $"Token inválido: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, string? userUid)> ValidateTokenForUid(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada")));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key,
                RequireExpirationTime = false
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userUid = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                        principal.FindFirst("uid")?.Value;

            Console.WriteLine($"UserUid encontrado no token: {userUid}");

            if (string.IsNullOrEmpty(userUid))
            {
                return (false, "Token inválido: UserUid não encontrado", null);
            }

            var response = await _supabaseClient
                .From<Usuario>()
                .Select("user_uid")
                .Where(u => u.UserUid == userUid)
                .Get();

            var usuario = response.Models.FirstOrDefault();
            Console.WriteLine($"Usuário encontrado: {(usuario != null ? "Sim" : "Não")}");

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", null);
            }

            return (true, "Token válido", usuario.UserUid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao validar token para userUid: {ex.Message}");
            return (false, $"Token inválido: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, PerfilResponseDTO? usuario)> ObterPerfil(string token)
    {
        try
        {
            var (success, message, userId) = await ValidateToken(token);
            if (!success)
            {
                return (false, message, null);
            }

            Console.WriteLine($"Buscando perfil para userId: {userId}");

            var response = await _supabaseClient
                .From<Usuario>()
                .Select("*")
                .Where(u => u.Id == userId)
                .Get();

            var usuarioDb = response.Models.FirstOrDefault();
            Console.WriteLine($"Perfil encontrado: {(usuarioDb != null ? "Sim" : "Não")}");

            if (usuarioDb == null)
            {
                return (false, "Usuário não encontrado", null);
            }

            var perfilResponse = new PerfilResponseDTO
            {
                Id = usuarioDb.Id,
                Nome = usuarioDb.Nome,
                Email = usuarioDb.Email,
                Telefone = usuarioDb.Telefone,
                Genero = usuarioDb.Genero ?? string.Empty,
                Localizacao = usuarioDb.Localizacao,
                Foto = usuarioDb.Foto,
                PontosTotais = usuarioDb.PontosTotais,
                DataNascimento = usuarioDb.DataNascimento ?? default(DateTime),
                Status = usuarioDb.Status,
                UltimoLogin = usuarioDb.UltimoLogin,
                UserUid = usuarioDb.UserUid
            };

            return (true, "Perfil obtido com sucesso", perfilResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter perfil: {ex.Message}");
            return (false, "Erro ao obter perfil do usuário", null);
        }
    }

    public async Task<(bool success, string message)> AtualizarPerfil(PerfilDTO perfilDTO)
    {
        try
        {
            var (success, message, userId) = await ValidateToken(perfilDTO.Token);
            if (!success)
            {
                return (false, message);
            }

            var usuario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado");
            }

            if (perfilDTO.Nome != null) usuario.Nome = perfilDTO.Nome;
            if (perfilDTO.Telefone != null) usuario.Telefone = perfilDTO.Telefone;
            if (perfilDTO.Genero != null) usuario.Genero = perfilDTO.Genero;
            if (perfilDTO.Localizacao != null) usuario.Localizacao = perfilDTO.Localizacao;

            await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Update(usuario);

            return (true, "Perfil atualizado com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar perfil: {ex.Message}");
            return (false, "Erro ao atualizar perfil do usuário");
        }
    }

    public async Task<(bool success, string message)> AtualizarSenha(SenhaDTO senhaDTO)
    {
        try
        {
            var (success, message, userId) = await ValidateToken(senhaDTO.Token);
            if (!success)
            {
                return (false, message);
            }

            var usuario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado");
            }

            if (!BCrypt.Net.BCrypt.Verify(senhaDTO.SenhaAtual, usuario.Senha))
            {
                return (false, "Senha atual incorreta");
            }

            var novaSenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaDTO.NovaSenha);
            usuario.Senha = novaSenhaHash;

            await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Update(usuario);

            return (true, "Senha atualizada com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar senha: {ex.Message}");
            return (false, "Erro ao atualizar senha do usuário");
        }
    }

    public async Task<(bool success, string message, string? fotoUrl)> AtualizarFoto(IFormFile foto, string userUid)
    {
        try
        {
            var usuario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.UserUid == userUid)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", null);
            }

            if (!foto.ContentType.StartsWith("image/"))
            {
                return (false, "O arquivo deve ser uma imagem", null);
            }

            var extensao = Path.GetExtension(foto.FileName);
            var nomeArquivo = $"usuario_{usuario.Id}_{DateTime.UtcNow.Ticks}{extensao}";

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await foto.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Usar o cliente admin para upload
            var uploadResult = await _supabaseAdminClient.Storage
                .From(BUCKET_FOTOS_PERFIL)
                .Upload(fileBytes, nomeArquivo);

            var fotoUrl = _supabaseAdminClient.Storage
                .From(BUCKET_FOTOS_PERFIL)
                .GetPublicUrl(nomeArquivo);

            usuario.Foto = fotoUrl;

            await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == usuario.Id)
                .Update(usuario);

            return (true, "Foto de perfil atualizada com sucesso", fotoUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar foto: {ex.Message}");
            return (false, "Erro ao atualizar foto de perfil", null);
        }
    }

    public async Task<(bool success, string message, long pontosAtuais)> AtualizarPontos(long userId, long pontosAdicionais)
    {
        try
        {
            if (pontosAdicionais <= 0)
            {
                return (false, "Quantidade de pontos inválida", 0);
            }

            var usuario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", 0);
            }

            usuario.PontosTotais += pontosAdicionais;

            await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Update(usuario);

            try
            {
                var carteira = await _supabaseClient
                    .From<CarteiraDigital>()
                    .Where(c => c.UsuarioId == userId)
                    .Single();

                if (carteira != null)
                {
                    carteira.Pontos += pontosAdicionais;

                    await _supabaseClient
                        .From<CarteiraDigital>()
                        .Where(c => c.UsuarioId == userId)
                        .Update(carteira);

                    Console.WriteLine($"Carteira digital atualizada com {pontosAdicionais} pontos para o usuário {userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar carteira digital: {ex.Message}");
            }

            return (true, "Pontos atualizados com sucesso", usuario.PontosTotais);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar pontos: {ex.Message}");
            return (false, "Erro ao atualizar pontos do usuário", 0);
        }
    }

    public async Task<(bool success, string message, long pontos)> GetUserWallet(long userId)
    {
        try
        {
            var carteira = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteira == null)
            {
                return (false, "Carteira digital não encontrada para o usuário", 0);
            }

            return (true, "Carteira digital encontrada", carteira.Pontos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter carteira do usuário: {ex.Message}");
            return (false, "Erro ao obter carteira do usuário", 0);
        }
    }

    public async Task<(bool success, string message, long pontosAtuais)> DeductPoints(long userId, long pontosDeduzidos)
    {
        try
        {
            if (pontosDeduzidos <= 0)
            {
                return (false, "Quantidade de pontos a deduzir inválida", 0);
            }

            var carteira = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteira == null)
            {
                return (false, "Carteira digital não encontrada para o usuário", 0);
            }

            if (carteira.Pontos < pontosDeduzidos)
            {
                return (false, "Usuário não tem pontos suficientes na carteira digital", carteira.Pontos);
            }

            carteira.Pontos -= pontosDeduzidos;

            await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Update(carteira);

            Console.WriteLine($"Carteira digital atualizada: {pontosDeduzidos} pontos deduzidos do usuário {userId}");

            return (true, "Pontos deduzidos com sucesso", carteira.Pontos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao deduzir pontos: {ex.Message}");
            return (false, "Erro ao deduzir pontos do usuário", 0);
        }
    }

    public async Task<Usuario?> ObterUsuarioPorId(long userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();
            
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter usuário por ID: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool success, string message)> SolicitarRecuperacaoSenha(string email)
    {
        try
        {
            var usuario = await _supabaseClient.From<Usuario>()
                .Where(u => u.Email == email)
                .Single();

            if (usuario == null)
                return (false, "Email não encontrado");

            var codigo = new Random().Next(10000000, 99999999).ToString();
            var expiresAt = DateTime.UtcNow.AddHours(1);

            await _supabaseClient.Rpc("inserir_recuperacao_senha", new { 
                p_user_id = usuario.Id, 
                p_codigo = codigo, 
                p_expires_at = expiresAt 
            });

            await SendRecoveryEmail(email, codigo);

            return (true, "Código de recuperação enviado para o email");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao solicitar recuperação de senha: {ex.Message}");
            return (false, "Erro ao solicitar recuperação de senha");
        }
    }

    public async Task<(bool success, string message)> RedefinirSenha(string codigo, string novaSenha)
    {
        try
        {
            var recuperacaoResponse = await _supabaseClient.From<RecuperacaoSenha>()
                .Match(new { codigo = codigo })
                .Get();

            if (recuperacaoResponse.Models == null || !recuperacaoResponse.Models.Any())
                return (false, "Código inválido");

            var recuperacao = recuperacaoResponse.Models.First();

            if (recuperacao.ExpiresAt != null && recuperacao.ExpiresAt < DateTime.UtcNow)
            {
                await _supabaseClient.From<RecuperacaoSenha>()
                    .Match(new { codigo = codigo })
                    .Delete();
                return (false, "Código expirado");
            }

            var usuario = await _supabaseClient.From<Usuario>()
                .Match(new { id = recuperacao.UserId })
                .Single();

            if (usuario == null)
                return (false, "Usuário não encontrado");

            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            await _supabaseClient.From<Usuario>()
                .Match(new { id = recuperacao.UserId })
                .Update(usuario);

            await _supabaseClient.From<RecuperacaoSenha>()
                .Match(new { codigo = codigo })
                .Delete();

            return (true, "Senha redefinida com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao redefinir senha: {ex.Message}");
            return (false, "Erro ao redefinir senha");
        }
    }

    private async Task SendRecoveryEmail(string email, string codigo)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("EcoIpil", _configuration["EmailSettings:SenderEmail"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Recuperação de Senha - EcoIpil";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <!DOCTYPE html>
                <html lang='pt-BR'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Recuperação de Senha - EcoIpil</title>
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
                    <div class='container'>
                        <div class='header'>
                            <h1>EcoIpil</h1>
                        </div>
                        <div class='content'>
                            <h2>Recuperação de Senha</h2>
                            <p>Olá! Recebemos uma solicitação para redefinir a senha da sua conta EcoIpil.</p>
                            <p>Para continuar, use o código abaixo no aplicativo:</p>
                            <div class='code-box'>{codigo}</div>
                            <p>Este código é válido por 1 hora. Se você não solicitou essa alteração, ignore este email ou entre em contato com nosso suporte.</p>
                        </div>
                        <div class='footer'>
                            <p>Precisa de ajuda? <a href='mailto:suporte@eco-ipil.com'>Entre em contato com o suporte</a></p>
                            <p>&copy; 2023 EcoIpil. Todos os direitos reservados.</p>
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

            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            Console.WriteLine($"Email de recuperação enviado para {email} com código {codigo}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar email de recuperação: {ex.Message}");
            throw;
        }
    }
}