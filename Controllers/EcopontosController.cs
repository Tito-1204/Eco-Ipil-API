using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.Services;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/ecopontos")]
public class EcopontosController : ControllerBase
{
    private readonly EcopontoService _ecopontoService;

    public EcopontosController(EcopontoService ecopontoService)
    {
        _ecopontoService = ecopontoService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarEcopontos(
        [FromQuery] float? latitude = null,
        [FromQuery] float? longitude = null,
        [FromQuery] float? raio = null,
        [FromQuery] string? material = null,
        [FromQuery] string? status = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 10)
    {
        var (success, message, ecopontos) = await _ecopontoService.ListarEcopontos(
            latitude, longitude, raio, material, status, pagina, limite);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = ecopontos,
            paginacao = new
            {
                pagina,
                limite,
                total = ecopontos?.Count ?? 0
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterEcoponto([FromRoute] long id)
    {
        var (success, message, ecoponto) = await _ecopontoService.ObterEcoponto(id);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = ecoponto
        });
    }
} 