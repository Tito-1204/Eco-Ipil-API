using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using System.Threading.Tasks;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/notificacoes")]
public class NotificacaoController : ControllerBase
{
    private readonly NotificacaoService _notificacaoService;
    private readonly ILogger<NotificacaoController> _logger;

    public NotificacaoController(NotificacaoService notificacaoService, ILogger<NotificacaoController> logger)
    {
        _notificacaoService = notificacaoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListarNotificacoes([FromQuery] string token, [FromQuery] string? lida, [FromQuery] int? pagina, [FromQuery] int? limite)
    {
        try
        {
            var (success, message, notificacoes) = await _notificacaoService.ListarNotificacoes(token, lida, pagina, limite);
            if (!success)
            {
                return BadRequest(new { status = false, message });
            }
            return Ok(new { status = true, message, data = notificacoes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar notificações");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }

    [HttpPut("{id}/ler")]
    public async Task<IActionResult> MarcarComoLida([FromQuery] string token, [FromRoute] long id)
    {
        try
        {
            var (success, message) = await _notificacaoService.MarcarComoLida(token, id);
            if (!success)
            {
                return BadRequest(new { status = false, message });
            }
            return Ok(new { status = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação como lida");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }

    [HttpPut("ler-todas")]
    public async Task<IActionResult> MarcarTodasComoLidas([FromQuery] string token)
    {
        try
        {
            var (success, message) = await _notificacaoService.MarcarTodasComoLidas(token);
            if (!success)
            {
                return BadRequest(new { status = false, message });
            }
            return Ok(new { status = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações como lidas");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }
}