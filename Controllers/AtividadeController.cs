using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.Services;
using System.Threading.Tasks;
using System.Linq;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/atividades")]
public class AtividadeController : ControllerBase
{
    private readonly AtividadeService _atividadeService;
    private readonly UsuarioService _usuarioService;
    private readonly ILogger<AtividadeController> _logger;

    public AtividadeController(
        AtividadeService atividadeService,
        UsuarioService usuarioService,
        ILogger<AtividadeController> logger)
    {
        _atividadeService = atividadeService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet("recentes")]
    public async Task<IActionResult> GetRecentActivities([FromQuery] string token)
    {
        try
        {
            // Validar o token e obter o ID do usuário logado
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { status = false, message = "Token de autenticação não fornecido" });
            }

            var (tokenValid, tokenMessage, userId) = await _usuarioService.ValidateToken(token);
            if (!tokenValid)
            {
                return Unauthorized(new { status = false, message = tokenMessage });
            }

            // Buscar as atividades recentes do usuário
            var activities = await _atividadeService.GetRecentActivities(userId);

            return Ok(new
            {
                status = true,
                message = "Atividades recentes obtidas com sucesso",
                data = activities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter atividades recentes");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }
}