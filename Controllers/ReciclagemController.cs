using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using System.Text.Json;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/reciclagem")]
public class ReciclagemController : ControllerBase
{
    private readonly ReciclagemService _reciclagemService;

    public ReciclagemController(ReciclagemService reciclagemService)
    {
        _reciclagemService = reciclagemService;
    }

    [HttpPost("escanear")]
    public async Task<IActionResult> EscanearQR([FromBody] EscanearQRRequestDTO request)
    {
        try
        {
            var (success, message, data) = await _reciclagemService.EscanearQR(request.Token, request.CodigoQR);
            if (success)
            {
                return Ok(new { status = true, message, data });
            }
            return BadRequest(new { status = false, message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao processar código QR: {ex.Message}" });
        }
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> RegistrarReciclagem([FromBody] ReciclagemRequestDTO request)
    {
        var (success, message, reciclagem) = await _reciclagemService.RegistrarReciclagem(
            request.Token,
            request.MaterialId,
            request.Peso,
            request.EcopontoId,
            request.Qualidade ?? string.Empty,
            request.AgenteId);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new { status = true, message, data = reciclagem });
    }

    [HttpPost("avaliar")]
    public async Task<IActionResult> AvaliarReciclagem([FromBody] AvaliarReciclagemRequestDTO request)
    {
        try
        {
            var (success, message, data) = await _reciclagemService.AvaliarReciclagem(
                request.Token,
                request.Rating,
                request.Comentario);

            if (success)
            {
                var options = new JsonSerializerOptions
                {
                    IgnoreReadOnlyProperties = true,
                    PropertyNameCaseInsensitive = true
                };
                return Ok(JsonSerializer.Serialize(new { status = true, message, data }, options));
            }
            return BadRequest(new { status = false, message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao processar avaliação: {ex.Message}" });
        }
    }
}