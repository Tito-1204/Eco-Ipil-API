using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.Services;
using EcoIpil.API.DTOs;
using System.Threading.Tasks;
using System;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketController : ControllerBase
{
    private readonly TicketService _ticketService;

    public TicketController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Lista os tickets do usuário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarTickets(
        [FromQuery] string token,
        [FromQuery] string? status,
        [FromQuery] int? pagina,
        [FromQuery] int? limite)
    {
        var (success, message, tickets) = await _ticketService.ListarTickets(token, status, pagina, limite);
        if (success)
        {
            return Ok(new { status = true, data = tickets, message });
        }
        return BadRequest(new { status = false, message });
    }

    /// <summary>
    /// Obtém detalhes de um ticket específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterTicket([FromRoute] long id, [FromQuery] string token)
    {
        var (success, message, ticket) = await _ticketService.ObterTicket(token, id);
        if (success)
        {
            return Ok(new { status = true, data = ticket, message });
        }
        return BadRequest(new { status = false, message });
    }

    /// <summary>
    /// Gera um novo ticket
    /// </summary>
    [HttpPost("gerar")]
    public async Task<IActionResult> GerarTicket([FromBody] TicketCreateDTO ticketDTO)
    {
        var (success, message, ticket) = await _ticketService.CriarTicket(ticketDTO);
        if (success)
        {
            return Ok(new { status = true, data = ticket, message });
        }
        return BadRequest(new { status = false, message });
    }

    /// <summary>
    /// Gera um PDF para um ticket de pagamento a mão, armazena no bucket do Supabase e retorna o PDF e o link de download
    /// </summary>
    [HttpPost("{id}/pdf")]
    public async Task<IActionResult> GerarPdfTicket([FromRoute] long id, [FromBody] BaseRequestDTO request)
    {
        try
        {
            var (success, message, pdfBytes, downloadUrl) = await _ticketService.GerarPdfTicket(request.Token, id);

            if (!success)
            {
                return BadRequest(new { status = false, message });
            }

            // Converter o PDF para base64 para visualização no navegador
            var pdfBase64 = Convert.ToBase64String(pdfBytes!);

            // Retornar uma resposta JSON com o PDF em base64 e o link de download
            return Ok(new
            {
                status = true,
                message = "PDF gerado, armazenado e disponível para visualização",
                data = new
                {
                    pdfBase64 = pdfBase64, // Para visualização no navegador
                    downloadUrl = downloadUrl // Link para download
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = false, message = $"Erro interno: {ex.Message}" });
        }
    }
}