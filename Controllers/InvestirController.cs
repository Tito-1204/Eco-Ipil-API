using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using System.Threading.Tasks;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/investir")]
public class InvestirController : ControllerBase
{
    private readonly InvestirService _investirService;
    private readonly UsuarioService _usuarioService; // Adicionado para validação do token
    private readonly ILogger<InvestirController> _logger;

    public InvestirController(
        InvestirService investirService,
        UsuarioService usuarioService, // Adicionado no construtor
        ILogger<InvestirController> logger)
    {
        _investirService = investirService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> MakeInvestment([FromBody] InvestirRequestDTO request)
    {
        try
        {
            // Validar o token
            if (string.IsNullOrEmpty(request.Token))
            {
                return Unauthorized(new { status = false, message = "Token de autenticação não fornecido" });
            }

            var (tokenValid, tokenMessage, userId) = await _usuarioService.ValidateToken(request.Token);
            if (!tokenValid)
            {
                return Unauthorized(new { status = false, message = tokenMessage });
            }

            // Verificar se o userId do token corresponde ao userId da requisição
            if (userId != request.UserId)
            {
                return Unauthorized(new { status = false, message = "Token não corresponde ao usuário informado" });
            }

            var (success, message) = await _investirService.MakeInvestment(request.UserId, request.InvestimentoId, request.PontosInvestidos);
            if (success)
            {
                return Ok(new { status = true, message });
            }
            else
            {
                return BadRequest(new { status = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar investimento para o usuário {UserId}", request.UserId);
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }

    [HttpPost("apply-returns")]
    public async Task<IActionResult> ApplyReturns([FromQuery] string token)
    {
        try
        {
            // Validar o token
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { status = false, message = "Token de autenticação não fornecido" });
            }

            var (success, message) = await _investirService.ApplyReturns(token); // Passa o token para o serviço
            if (success)
            {
                return Ok(new { status = true, message });
            }
            else
            {
                return BadRequest(new { status = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aplicar retornos");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }
}