using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;

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
            if (request == null)
            {
                return BadRequest(new { status = false, message = "Requisição inválida." });
            }

            if (string.IsNullOrWhiteSpace(request.CodigoQR))
            {
                return BadRequest(new { status = false, message = "O código QR é obrigatório." });
            }

            var (success, message, data) = await _reciclagemService.EscanearQR(request.Token, request.CodigoQR);
            if (success)
            {
                return Ok(new
                {
                    status = true,
                    message,
                    data = data ?? new { registroReciclagem = (object?)null, detalhes = (object?)null }
                });
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
        try
        {
            var (success, message, reciclagem) = await _reciclagemService.RegistrarReciclagem(
                request.Token,
                request.MaterialId,
                request.Peso,
                request.EcopontoId,
                request.Qualidade ?? string.Empty,
                request.AgenteId);

            if (!success)
            {
                return BadRequest(new { status = false, message });
            }

            var safeReciclagem = reciclagem == null ? null : new
            {
                id = reciclagem.Id,
                createdAt = reciclagem.CreatedAt,
                peso = reciclagem.Peso,
                usuarioId = reciclagem.UsuarioId,
                materialId = reciclagem.MaterialId,
                ecopontoId = reciclagem.EcopontoId,
                agenteId = reciclagem.AgenteId,
                materialNome = reciclagem.MaterialNome,
                materialClasse = reciclagem.MaterialClasse,
                pontosGanhos = reciclagem.PontosGanhos,
                ecopontoNome = reciclagem.EcopontoNome,
                ecopontoLocalizacao = reciclagem.EcopontoLocalizacao,
                agenteNome = reciclagem.AgenteNome
            };

            return Ok(new { status = true, message, data = safeReciclagem });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao registrar reciclagem: {ex.Message}" });
        }
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
                var safeData = data == null ? new { id = (long?)null, status = (string?)null } : data;
                return Ok(new { status = true, message, data = safeData });
            }

            return BadRequest(new { status = false, message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao processar avaliação: {ex.Message}" });
        }
    }
}