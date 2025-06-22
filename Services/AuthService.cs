using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Linq;
using Supabase.Postgrest.Models;
using Supabase;
using Supabase.Gotrue;
using System.Net.Mail;
using System.Net;

namespace EcoIpil.API.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly SupabaseService _supabaseService;
        private static readonly Regex EmailRegex = new Regex(@"^[^\s@]+@[^\s@]{1,20}\.[a-zA-Z]{2,3}$", RegexOptions.Compiled);
        private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*\d)[^\s]{8,}$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^\+244\d{9}$", RegexOptions.Compiled);
        
        private static readonly HashSet<string> DominiosConfiáveis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com",
            "hotmail.com",
            "outlook.com",
            "yahoo.com",
            "icloud.com",
            "aol.com",
            "protonmail.com",
            "mail.com",
            "zoho.com",
            "yandex.com"
        };

        public AuthService(IConfiguration configuration, SupabaseService supabaseService)
        {
            _configuration = configuration;
            _supabaseService = supabaseService;
        }

        public async Task<(bool success, string message, string? token)> Login(LoginDTO loginDto)
        {
            try
            {
                loginDto.Email = loginDto.Email?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(loginDto.Email) || !EmailRegex.IsMatch(loginDto.Email))
                    return (false, "Email inválido", null);

                var client = _supabaseService.GetClient();
                string userId = string.Empty;
                string userEmail = loginDto.Email;

                try
                {
                    var session = await client.Auth.SignIn(loginDto.Email, loginDto.Senha);
                    if (session != null && session.User != null)
                    {
                        userId = session.User.Id ?? string.Empty;
                        userEmail = session.User.Email ?? loginDto.Email;
                        Console.WriteLine($"Autenticação normal bem-sucedida para o usuário: {userEmail}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Autenticação normal falhou: {ex.Message}. Prosseguindo para autenticação alternativa.");
                }

                Console.WriteLine($"Tentando login alternativo para o email: {loginDto.Email}");
                var response = await client.From<Usuario>()
                    .Where(u => u.Email == loginDto.Email)
                    .Get();

                if (!response.Models.Any())
                {
                    Console.WriteLine($"Nenhum usuário encontrado com o email: {loginDto.Email}");
                    return (false, "Email ou senha incorretos", null);
                }

                var usuario = response.Models.First();
                Console.WriteLine($"Usuário encontrado: {usuario.Nome}, verificando senha");

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.Senha))
                {
                    Console.WriteLine("Senha incorreta");
                    return (false, "Email ou senha incorretos", null);
                }

                Console.WriteLine("Senha verificada com sucesso");

                try
                {
                    usuario.UltimoLogin = DateTime.UtcNow;
                    await client.From<Usuario>()
                        .Where(u => u.Id == usuario.Id)
                        .Update(usuario);
                    Console.WriteLine("Último login atualizado com sucesso");
                }
                catch (Exception updateEx)
                {
                    Console.WriteLine($"Erro ao atualizar último login: {updateEx.Message}");
                }

                var token = GerarToken(usuario);
                return (true, "Login realizado com sucesso", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro geral no login: {ex.Message}");
                return (false, $"Erro ao realizar login: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> Register(RegisterDTO registerDto)
        {
            try
            {
                registerDto.Nome = registerDto.Nome?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(registerDto.Nome) || registerDto.Nome.Split(' ').Length < 2)
                {
                    return (false, "Digite seu nome completo (primeiro e último nome)");
                }

                registerDto.Email = registerDto.Email?.Trim().ToLower() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(registerDto.Email) || !EmailRegex.IsMatch(registerDto.Email))
                {
                    return (false, "Formato de email inválido. Use um email válido como exemplo@dominio.com");
                }

                if (!PhoneRegex.IsMatch(registerDto.Telefone))
                {
                    return (false, "Telefone inválido. Use o formato: +244 XXX XXX XXX");
                }

                if (string.IsNullOrWhiteSpace(registerDto.Senha) || !PasswordRegex.IsMatch(registerDto.Senha))
                {
                    return (false, "A senha deve ter no mínimo 8 caracteres, uma letra maiúscula e um número");
                }

                if (registerDto.DataNascimento == default(DateTime))
                {
                    return (false, "Data de nascimento não pode estar vazia");
                }

                if (string.IsNullOrWhiteSpace(registerDto.Genero))
                {
                    return (false, "Selecione um gênero");
                }
                if (string.IsNullOrWhiteSpace(registerDto.Email) || registerDto.Email.Contains(" ") || registerDto.Email.StartsWith(" "))
                {
                    return (false, "O email não pode conter espaços ou estar vazio.");
                }

                var client = _supabaseService.GetClient();
                
                var existingUser = await client.From<Usuario>()
                    .Where(u => u.Email == registerDto.Email)
                    .Get();
                
                if (existingUser.Models.Any())
                {
                    return (false, "Este email já está registrado");
                }
                
                string dominio = registerDto.Email.Split('@').Last();
                bool usarSupabaseAuth = DominiosConfiáveis.Contains(dominio);
                string userUid = string.Empty;
                
                if (usarSupabaseAuth)
                {
                    try
                    {
                        Console.WriteLine($"Tentando registrar usuário com email confiável: {registerDto.Email}");
                        var userOptions = new SignUpOptions
                        {
                            Data = new Dictionary<string, object>
                            {
                                { "display_name", registerDto.Nome }
                            }
                        };
                        var authResponse = await client.Auth.SignUp(registerDto.Email, registerDto.Senha, userOptions);
                        if (authResponse != null && authResponse.User != null)
                        {
                            userUid = authResponse.User.Id ?? string.Empty;
                            if (string.IsNullOrEmpty(userUid))
                            {
                                Console.WriteLine("Erro ao obter ID do usuário do Supabase Auth");
                                usarSupabaseAuth = false;
                            }
                            else
                            {
                                Console.WriteLine($"Usuário criado com sucesso no Supabase Auth. ID: {userUid}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Resposta nula do Supabase Auth");
                            usarSupabaseAuth = false;
                        }
                    }
                    catch (Exception authEx)
                    {
                        Console.WriteLine($"Erro ao criar usuário no Supabase Auth: {authEx.Message}");
                        usarSupabaseAuth = false;
                    }
                }
                
                try
                {
                    if (!usarSupabaseAuth || string.IsNullOrEmpty(userUid))
                    {
                        userUid = "00000000-0000-0000-0000-000000000000";
                    }
                    
                    var usuario = new Usuario
                    {
                        Nome = registerDto.Nome,
                        Email = registerDto.Email,
                        Senha = BCrypt.Net.BCrypt.HashPassword(registerDto.Senha),
                        Telefone = registerDto.Telefone,
                        Genero = registerDto.Genero,
                        DataNascimento = registerDto.DataNascimento,
                        Status = "Ativo",
                        PontosTotais = 0,
                        CreatedAt = DateTime.UtcNow,
                        UserUid = userUid
                    };
                    
                    try
                    {
                        Console.WriteLine("Tentando inserir usuário sem especificar ID");
                        await client.From<Usuario>().Insert(usuario);
                        Console.WriteLine("Usuário inserido com sucesso");
                        
                        return (true, "Usuário registrado com sucesso!");
                    }
                    catch (Exception insertEx)
                    {
                        Console.WriteLine($"Erro ao inserir usuário: {insertEx.Message}");
                        if (insertEx.Message.Contains("duplicate key") || insertEx.Message.Contains("23505"))
                        {
                            Console.WriteLine("Tentando inserção alternativa sem especificar ID");
                            try
                            {
                                var novoUsuario = new Usuario
                                {
                                    Nome = usuario.Nome,
                                    Email = usuario.Email,
                                    Senha = usuario.Senha,
                                    Telefone = usuario.Telefone,
                                    Genero = usuario.Genero,
                                    DataNascimento = usuario.DataNascimento,
                                    Status = usuario.Status,
                                    PontosTotais = usuario.PontosTotais,
                                    CreatedAt = usuario.CreatedAt,
                                    UserUid = usuario.UserUid
                                };
                                await client.From<Usuario>().Insert(novoUsuario);
                                Console.WriteLine("Usuário inserido com novo objeto");
                                
                                return (true, "Usuário registrado com sucesso!");
                            }
                            catch (Exception altEx)
                            {
                                Console.WriteLine($"Erro na inserção alternativa: {altEx.Message}");
                                try
                                {
                                    Random random = new Random();
                                    usuario.Id = -random.Next(1, 1000000);
                                    await client.From<Usuario>().Insert(usuario);
                                    Console.WriteLine("Usuário inserido com ID negativo");
                                    
                                    
                                    return (true, "Usuário registrado com sucesso!");
                                }
                                catch (Exception negIdEx)
                                {
                                    Console.WriteLine($"Erro na inserção com ID negativo: {negIdEx.Message}");
                                    return (false, $"Não foi possível registrar o usuário. Erro: {negIdEx.Message}");
                                }
                            }
                        }
                        return (false, $"Erro ao registrar usuário: {insertEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao inserir na tabela usuarios: {ex.Message}");
                    return (false, $"Erro ao registrar usuário: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro geral: {ex.ToString()}");
                return (false, $"Erro ao registrar usuário: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> SendWelcomeEmail(string email, string nome)
        {
            try
            {
                var codigo = GenerateVerificationCode();
                var client = _supabaseService.GetClient();
                
                var codigoVerificacao = new CodigoVerificacao
                {
                    Email = email,
                    Codigo = codigo,
                    Tipo = "welcome",
                    CriadoEm = DateTime.UtcNow,
                    ExpiraEm = DateTime.UtcNow.AddHours(24)
                };

                await client.From<CodigoVerificacao>().Insert(codigoVerificacao);

                var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
                {
                    Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                    Credentials = new NetworkCredential(
                        _configuration["EmailSettings:SenderEmail"],
                        _configuration["EmailSettings:SenderPassword"]),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:SenderEmail"], _configuration["EmailSettings:SenderName"]),
                    Subject = "Bem-vindo ao Eco-Ipil!",
                    IsBodyHtml = true,
                    Body = $@"<!DOCTYPE html>
<html lang='pt'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(90deg, #2ecc71, #27ae60); padding: 20px; text-align: center; }}
        .header h1 {{ color: #ffffff; font-size: 24px; margin: 0; }}
        .content {{ padding: 20px; color: #333333; }}
        .content h2 {{ color: #2ecc71; font-size: 20px; }}
        .content p {{ font-size: 16px; line-height: 1.6; }}
        .code {{ background-color: #e0e0e0; padding: 10px; border-radius: 5px; text-align: center; font-size: 18px; font-weight: bold; letter-spacing: 2px; margin: 20px 0; color: #041c34; }}
        .footer {{ background-color: #041c34; padding: 10px; text-align: center; color: #e0e0e0; font-size: 12px; }}
        .footer a {{ color: #2ecc71; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Eco-Ipil</h1>
        </div>
        <div class='content'>
            <h2>Olá, {nome}!</h2>
            <p>Bem-vindo ao <strong>Eco-Ipil</strong>, o teu parceiro na missão de tornar o mundo mais verde! Estamos bué felizes por teres juntado a nossa comunidade de recicladores. 🌍</p>
            <p>Para confirmar que este e-mail é mesmo teu, usa o código abaixo:</p>
            <div class='code'>{codigo.Substring(0, 4)}-{codigo.Substring(4, 4)}</div>
            <p>Este código expira em 24 horas, por isso usa-o logo! Se precisares de ajuda, é só contactar-nos.</p>
            <p>Juntos, vamos fazer a diferença! 🚀</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Eco-Ipil. Todos os direitos reservados.</p>
            <p><a href='https://eco-ipil.com'>Visita o nosso site</a></p>
        </div>
    </div>
</body>
</html>"
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return (true, "E-mail de boas-vindas enviado com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar e-mail de boas-vindas: {ex.Message}");
                return (false, $"Erro ao enviar e-mail de boas-vindas: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> VerifyEmailCode(string email, string codigo)
        {
            try
            {
                email = email?.Trim().ToLower() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
                {
                    return (false, "Email inválido");
                }

                if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 9 || codigo[4] != '-')
                {
                    return (false, "Código inválido. Use o formato XXXX-XXXX");
                }

                var codigoLimpo = codigo.Replace("-", "");
                var client = _supabaseService.GetClient();
                var response = await client.From<CodigoVerificacao>()
                    .Where(c => c.Email == email && c.Codigo == codigoLimpo && c.Tipo == "welcome")
                    .Get();

                if (!response.Models.Any())
                {
                    return (false, "Código inválido ou não encontrado");
                }

                var codigoVerificacao = response.Models.First();
                if (codigoVerificacao.ExpiraEm < DateTime.UtcNow)
                {
                    return (false, "Código expirado");
                }

                await client.From<CodigoVerificacao>()
                    .Where(c => c.Id == codigoVerificacao.Id)
                    .Delete();

                return (true, "E-mail verificado com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar código: {ex.Message}");
                return (false, $"Erro ao verificar código: {ex.Message}");
            }
        }

        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string GerarToken(Usuario usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UserUid ?? ""),
                new Claim(ClaimTypes.Email, usuario.Email ?? ""),
                new Claim("id_numerico", usuario.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(30);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<long?> ObterIdDoToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada")));
                
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
                
                var userIdClaim = principal.FindFirst("id_numerico")?.Value;
                if (long.TryParse(userIdClaim, out long userId) && userId > 0)
                {
                    return userId;
                }

                var userUidClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userUidClaim))
                {
                     var client = _supabaseService.GetClient();
                     var response = await client.From<Usuario>()
                        .Select("id")
                        .Where(u => u.UserUid == userUidClaim)
                        .Single();
                    
                    if(response != null)
                    {
                        return response.Id;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter ID do token: {ex.Message}");
                return null;
            }
        }
    }
}