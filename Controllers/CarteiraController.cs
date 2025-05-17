using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/carteira")]
public class CarteiraController : ControllerBase
{
    private readonly CarteiraService _carteiraService;

    public CarteiraController(CarteiraService carteiraService)
    {
        _carteiraService = carteiraService;
    }

    [HttpPost("consultar")]
    public async Task<IActionResult> ObterCarteira([FromBody] BaseRequestDTO request)
    {
        var (success, message, carteira) = await _carteiraService.ObterCarteira(request.Token);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = carteira
        });
    }

    [HttpPost("transferir")]
    public async Task<IActionResult> Transferir([FromBody] TransferenciaDTO dto)
    {
        var (success, message) = await _carteiraService.Transferir(dto);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message
        });
    }

    [HttpPost("trocar-pontos")]
    public async Task<IActionResult> TrocarPontosPorSaldo([FromBody] TrocaPontosDTO dto)
    {
        if (string.IsNullOrEmpty(dto.Token))
            return BadRequest(new { status = false, message = "Token não pode ser nulo ou vazio." });

        var (success, message) = await _carteiraService.TrocarPontosPorSaldo(dto.Token, dto.Pontos);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message
        });
    }
}
