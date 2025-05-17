using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using System.Threading.Tasks;
using System;

namespace EcoIpil.API.Controllers;

/// <summary>
/// Controlador para gerenciamento de recompensas
/// </summary>
[ApiController]
[Route("api/v1/recompensas")]
public class RecompensasController : ControllerBase
{
    private readonly RecompensaService _recompensaService;

    public RecompensasController(RecompensaService recompensaService)
    {
        _recompensaService = recompensaService;
    }

    /// <summary>
    /// Lista todas as recompensas disponíveis
    /// </summary>
    /// <param name="token">Token de autenticação</param>
    /// <param name="tipo">Filtro por tipo de recompensa</param>
    /// <param name="precoMin">Filtro por preço mínimo em pontos</param>
    /// <param name="precoMax">Filtro por preço máximo em pontos</param>
    /// <returns>Lista de recompensas</returns>
    [HttpGet]
    public async Task<IActionResult> ListarRecompensas(
        [FromQuery] string token,
        [FromQuery] string? tipo = null,
        [FromQuery] string? precoMin = null,
        [FromQuery] string? precoMax = null)
    {
        try
        {
            // Converter os valores string para long?
            long? precoMinValue = null;
            if (!string.IsNullOrEmpty(precoMin) && long.TryParse(precoMin, out long minValue))
            {
                precoMinValue = minValue;
            }

            long? precoMaxValue = null;
            if (!string.IsNullOrEmpty(precoMax) && long.TryParse(precoMax, out long maxValue))
            {
                precoMaxValue = maxValue;
            }

            var (success, message, recompensas) = await _recompensaService.ListarRecompensas(
                token,
                tipo,
                precoMinValue,
                precoMaxValue);

            if (success)
            {
                return Ok(new
                {
                    status = true,
                    message,
                    data = recompensas
                });
            }
            else
            {
                return BadRequest(new
                {
                    status = false,
                    message
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                status = false,
                message = $"Erro ao listar recompensas: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtém os detalhes de uma recompensa específica
    /// </summary>
    /// <param name="id">ID da recompensa</param>
    /// <param name="token">Token de autenticação</param>
    /// <returns>Detalhes da recompensa</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterRecompensa(
        long id,
        [FromQuery] string token)
    {
        var (success, message, recompensa) = await _recompensaService.ObterRecompensa(
            token,
            id);

        if (success)
        {
            return Ok(new
            {
                status = true,
                message,
                data = recompensa
            });
        }
        else
        {
            return BadRequest(new
            {
                status = false,
                message
            });
        }
    }

    /// <summary>
    /// Resgata uma recompensa para o usuário
    /// </summary>
    /// <param name="id">ID da recompensa a ser resgatada</param>
    /// <param name="request">Token de autenticação</param>
    /// <returns>Resultado do resgate</returns>
    [HttpPost("{id}/resgatar")]
    public async Task<IActionResult> ResgatarRecompensa(
        long id,
        [FromBody] BaseRequestDTO request)
    {
        var (success, message, data) = await _recompensaService.ResgatarRecompensa(
            request.Token,
            id);

        if (success)
        {
            return Ok(new
            {
                status = true,
                message,
                data
            });
        }
        else
        {
            return BadRequest(new
            {
                status = false,
                message
            });
        }
    }

    /// <summary>
    /// Lista as recompensas resgatadas pelo usuário
    /// </summary>
    /// <param name="token">Token de autenticação</param>
    /// <param name="status">Filtro por status da recompensa</param>
    /// <returns>Lista de recompensas do usuário</returns>
    [HttpGet("usuario")]
    public async Task<IActionResult> ListarRecompensasUsuario(
        [FromQuery] string token,
        [FromQuery] string? status = null)
    {
        var (success, message, recompensas) = await _recompensaService.ListarRecompensasUsuario(
            token,
            status);

        if (success)
        {
            return Ok(new
            {
                status = true,
                message,
                data = recompensas
            });
        }
        else
        {
            return BadRequest(new
            {
                status = false,
                message
            });
        }
    }
} 