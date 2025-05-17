using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/perfil")]
public class PerfilController : ControllerBase
{
    private readonly PerfilService _perfilService;

    public PerfilController(PerfilService perfilService)
    {
        _perfilService = perfilService;
    }

    [HttpGet("pontos-totais")]
    public async Task<IActionResult> ObterPontosTotais([FromQuery] string token)
    {
        var (success, message, pontosTotais) = await _perfilService.ObterPontosTotais(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = pontosTotais });
    }

    [HttpGet("total-reciclado")]
    public async Task<IActionResult> ObterTotalReciclado([FromQuery] string token)
    {
        var (success, message, totalReciclado) = await _perfilService.ObterTotalReciclado(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = totalReciclado });
    }

    [HttpGet("co2-evitado")]
    public async Task<IActionResult> ObterCO2Evitado([FromQuery] string token)
    {
        var (success, message, co2Evitado) = await _perfilService.ObterCO2Evitado(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = co2Evitado });
    }

    [HttpGet("estatisticas")]
    public async Task<IActionResult> ObterEstatisticasUsuario([FromQuery] string token)
    {
        var (success, message, dados) = await _perfilService.ObterEstatisticasUsuario(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = dados });
    }

    [HttpGet("nivel")]
    public async Task<IActionResult> ObterNivelUsuario([FromQuery] string token)
    {
        var (success, message, nivel) = await _perfilService.ObterNivelUsuario(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = nivel });
    }

    [HttpGet("completo")]
    public async Task<IActionResult> ObterPerfilUsuario([FromQuery] string token)
    {
        var (success, message, dados) = await _perfilService.ObterPerfilUsuario(token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = dados });
    }
}