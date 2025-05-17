using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.Services;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/materiais")]
public class MateriaisController : ControllerBase
{
    private readonly MaterialService _materialService;

    public MateriaisController(MaterialService materialService)
    {
        _materialService = materialService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarMateriais([FromQuery] string? classe = null)
    {
        var (success, message, materiais) = await _materialService.ListarMateriais(classe);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = materiais
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterMaterial([FromRoute] long id)
    {
        var (success, message, material) = await _materialService.ObterMaterial(id);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = material
        });
    }
} 