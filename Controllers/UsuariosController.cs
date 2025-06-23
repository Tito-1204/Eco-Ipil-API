using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using EcoIpil.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarioService;
    private readonly AuthService _authService;
    private readonly Client _supabaseClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        UsuarioService usuarioService,
        AuthService authService,
        SupabaseService supabaseService,
        IConfiguration configuration,
        ILogger<UsuariosController> logger)
    {
        _usuarioService = usuarioService;
        _authService = authService;
        _supabaseClient = supabaseService.GetClient();
        _configuration = configuration;
        _logger = logger;
    }

    private string ComputeCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    [HttpGet("google-login")]
    public async Task<IActionResult> GoogleLogin()
    {
        try
        {
            var codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var codeChallenge = ComputeCodeChallenge(codeVerifier);

            var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            HttpContext.Session.SetString("code_verifier", codeVerifier);
            HttpContext.Session.SetString("oauth_state", state);

            var redirectUri = $"https://eco-ipil-api-production.up.railway.app/api/v1/usuarios/google-callback?custom_state={Uri.EscapeDataString(state)}";

            _logger.LogInformation("Iniciando login com Google usando o cliente Supabase, redirectUri: {RedirectUri}", redirectUri);

            var options = new Supabase.Gotrue.SignInOptions
            {
                RedirectTo = redirectUri,
                Scopes = "openid email profile",
                FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE
            };

            await _supabaseClient.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                options
            );

            var supabaseUrl = _configuration["Supabase:Url"] ?? "https://ffzjllblyilfmrdvfege.supabase.co";
            var authUrl = $"{supabaseUrl}/auth/v1/authorize?provider=google&redirect_to={Uri.EscapeDataString(redirectUri)}&scopes={Uri.EscapeDataString("openid email profile")}&response_type=code&code_challenge={codeChallenge}&code_challenge_method=S256";

            _logger.LogInformation("URL de autorização gerada: {AuthUrl}", authUrl);

            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar o login com Google");
            return StatusCode(500, new { status = false, message = $"Erro ao iniciar o login com Google: {ex.Message}" });
        }
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? custom_state, [FromQuery] string? error, [FromQuery] string? error_description)
    {
        _logger.LogInformation("⚡ CALLBACK do Google acionado! code={Code}, custom_state={CustomState}, error={Error}", code, custom_state, error);
        try
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("Erro no callback do Google: {Error}, Descrição: {ErrorDescription}", error, error_description);
                return BadRequest(new { status = false, message = "Erro no fluxo de autenticação", details = new { error, error_description } });
            }

            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Código de autorização ausente no callback do Google");
                return BadRequest(new { status = false, message = "Código de autorização ausente" });
            }

            if (string.IsNullOrEmpty(custom_state))
            {
                _logger.LogWarning("Parâmetro custom_state ausente no callback do Google");
                return BadRequest(new { status = false, message = "Parâmetro custom_state ausente" });
            }

            var storedState = HttpContext.Session.GetString("oauth_state");
            if (string.IsNullOrEmpty(storedState) || storedState != custom_state)
            {
                _logger.LogWarning("Estado OAuth inválido. Custom state recebido: {CustomState}, State armazenado: {StoredState}", custom_state, storedState);
                return BadRequest(new { status = false, message = "Estado OAuth inválido. Possível ataque CSRF." });
            }

            var codeVerifier = HttpContext.Session.GetString("code_verifier");
            if (string.IsNullOrEmpty(codeVerifier))
            {
                _logger.LogWarning("Code verifier não encontrado na sessão");
                return BadRequest(new { status = false, message = "Code verifier não encontrado na sessão" });
            }

            _logger.LogInformation("Code verifier recuperado: {CodeVerifier}", codeVerifier);

            _logger.LogInformation("Trocando código por access_token usando o cliente Supabase, code: {Code}", code);
            var sessionResponse = await _supabaseClient.Auth.ExchangeCodeForSession(codeVerifier, code);

            if (sessionResponse == null || string.IsNullOrEmpty(sessionResponse.AccessToken))
            {
                _logger.LogWarning("Falha ao trocar o código por um token de acesso usando o cliente Supabase");
                return BadRequest(new { status = false, message = "Falha ao trocar o código por um token de acesso usando o cliente Supabase" });
            }

            var accessToken = sessionResponse.AccessToken;
            var refreshToken = sessionResponse.RefreshToken;

            _logger.LogInformation("Access token obtido com sucesso: {AccessToken}", accessToken);

            var user = await _supabaseClient.Auth.GetUser(accessToken);
            if (user == null)
            {
                _logger.LogWarning("Token inválido ou usuário não encontrado");
                return BadRequest(new { status = false, message = "Token inválido ou usuário não encontrado" });
            }

            var usuarioExistente = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.UserUid == user.Id)
                .Single();

            if (usuarioExistente == null)
            {
                var novoUsuario = new Usuario
                {
                    UserUid = user.Id ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Nome = user.UserMetadata["full_name"]?.ToString() ?? user.Email?.Split('@')[0] ?? "Usuário",
                    Foto = user.UserMetadata["avatar_url"]?.ToString(),
                    Telefone = "",
                    Senha = "",
                    DataNascimento = DateTime.MinValue,
                    Status = "Pendente",
                    CreatedAt = DateTime.UtcNow,
                    PontosTotais = 0
                };

                await _supabaseClient.From<Usuario>().Insert(novoUsuario);

                var token = _authService.GerarToken(novoUsuario);
                _logger.LogInformation("Novo usuário criado: {UserId}, Email: {Email}", novoUsuario.Id, novoUsuario.Email);
                return Ok(new
                {
                    status = true,
                    message = "Usuário criado. Complete seu perfil com telefone, senha e data de nascimento.",
                    userId = novoUsuario.Id,
                    status_usuario = "pendente",
                    access_token = accessToken,
                    jwt_token = token
                });
            }
            else
            {
                usuarioExistente.UltimoLogin = DateTime.UtcNow;
                await _supabaseClient.From<Usuario>().Update(usuarioExistente);

                var token = _authService.GerarToken(usuarioExistente);
                _logger.LogInformation("Login realizado com sucesso para usuário: {UserId}, Email: {Email}", usuarioExistente.Id, usuarioExistente.Email);
                return Ok(new
                {
                    status = true,
                    message = "Login realizado com sucesso",
                    userId = usuarioExistente.Id,
                    status_usuario = "ativo",
                    access_token = accessToken,
                    jwt_token = token
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no callback do Google");
            return StatusCode(500, new { status = false, message = $"Erro no callback: {ex.Message}" });
        }
    }

    [HttpPut("completar-perfil")]
    public async Task<IActionResult> CompletarPerfil([FromBody] CompletarPerfilRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Dados inválidos ao completar perfil: {Errors}", errors);
                return BadRequest(new { status = false, message = "Dados inválidos", errors });
            }

            var (success, message, userId) = await _usuarioService.ValidateToken(request.Token);
            if (!success)
            {
                _logger.LogWarning("Falha ao validar token: {Message}", message);
                return BadRequest(new { status = false, message });
            }

            var usuario = await _supabaseClient
                .From<Usuario>()
                .Where(u => u.Id == userId)
                .Single();

            if (usuario == null)
            {
                _logger.LogWarning("Usuário não encontrado: {UserId}", userId);
                return NotFound(new { status = false, message = "Usuário não encontrado" });
            }

            usuario.Telefone = request.Telefone;
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha);
            usuario.DataNascimento = request.DataNascimento;
            usuario.Genero = request.Genero;
            usuario.Status = "Ativo";

            await _supabaseClient.From<Usuario>().Update(usuario);

            var token = _authService.GerarToken(usuario);
            _logger.LogInformation("Perfil completado com sucesso para usuário: {UserId}", userId);
            return Ok(new
            {
                status = true,
                message = "Perfil completado com sucesso",
                jwt_token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar perfil");
            return StatusCode(500, new { status = false, message = $"Erro ao completar perfil: {ex.Message}" });
        }
    }

    [HttpPost("perfil")]
    public async Task<IActionResult> ObterPerfil([FromBody] BaseRequestDTO request)
    {
        var (success, message, usuario) = await _usuarioService.ObterPerfil(request.Token);

        if (!success || usuario == null)
        {
            _logger.LogWarning("Falha ao obter perfil: {Message}", message);
            return BadRequest(new { status = false, message });
        }

        _logger.LogInformation("Perfil obtido com sucesso para usuário: {UserId}", usuario.Id);
        return Ok(new
        {
            status = true,
            message,
            data = usuario
        });
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> AtualizarPerfil([FromBody] PerfilDTO perfilDTO)
    {
        var (success, message) = await _usuarioService.AtualizarPerfil(perfilDTO);

        if (!success)
        {
            _logger.LogWarning("Falha ao atualizar perfil: {Message}", message);
            return BadRequest(new { status = false, message });
        }

        _logger.LogInformation("Perfil atualizado com sucesso");
        return Ok(new
        {
            status = true,
            message
        });
    }

    [HttpPut("senha")]
    public async Task<IActionResult> AtualizarSenha([FromBody] SenhaDTO senhaDTO)
    {
        var (success, message) = await _usuarioService.AtualizarSenha(senhaDTO);

        if (!success)
        {
            _logger.LogWarning("Falha ao atualizar senha: {Message}", message);
            return BadRequest(new { status = false, message });
        }

        _logger.LogInformation("Senha atualizada com sucesso");
        return Ok(new
        {
            status = true,
            message
        });
    }

    [HttpPost("foto")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AtualizarFoto([FromForm] FotoUploadDTO dto)
    {
        var (success, message, fotoUrl) = await _usuarioService.AtualizarFoto(dto.Foto, dto.UserUid);

        if (!success)
        {
            _logger.LogWarning("Falha ao atualizar foto: {Message}", message);
            return BadRequest(new { status = false, message });
        }

        _logger.LogInformation("Foto atualizada com sucesso para usuário: {UserUid}", dto.UserUid);
        return Ok(new
        {
            status = true,
            message,
            data = new { fotoUrl }
        });
    }

    [HttpPost("recuperar-senha")]
    public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaRequestDTO request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { status = false, message = "Email não pode ser nulo ou vazio." });
        }

        var (success, message) = await _usuarioService.SolicitarRecuperacaoSenha(request.Email);
        return success ? Ok(new { status = true, message })
                       : BadRequest(new { status = false, message });
    }

    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequestDTO request)
    {
        var (success, message) = await _usuarioService.RedefinirSenha(request.Codigo, request.NovaSenha);
        return success ? Ok(new { status = true, message })
                      : BadRequest(new { status = false, message });
    }
}