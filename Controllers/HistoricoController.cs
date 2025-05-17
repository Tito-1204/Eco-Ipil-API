using System;
using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/historico")]
public class HistoricoController : ControllerBase
{
    private readonly HistoricoService _historicoService;

    public HistoricoController(HistoricoService historicoService)
    {
        _historicoService = historicoService;
    }

    [HttpPost("estatisticas")]
    public async Task<IActionResult> ObterEstatisticas([FromBody] BaseRequestDTO request)
    {
        (bool success, string message, HistoricoEstatisticasDTO? estatisticas) = await _historicoService.ObterEstatisticas(request.Token);

        if (!success)
        {
            return BadRequest(new { status = false, message });
        }

        return Ok(new { status = true, data = estatisticas });
    }

    [HttpPost("grafico")]
    public async Task<IActionResult> ObterDadosGrafico([FromBody] GraficoRequestDTO request)
    {
        (bool success, string message, HistoricoGraficoDTO? grafico) = await _historicoService.ObterDadosGrafico(
            request.Token,
            request.Periodo,
            request.Ano ?? DateTime.UtcNow.Year,  // Usa o ano atual se nulo,
            request.Mes);

        if (!success)
        {
            return BadRequest(new { status = false, message });
        }

        return Ok(new { status = true, data = grafico });
    }

    [HttpPost("materiais-reciclados")]
    public async Task<IActionResult> ObterMateriaisReciclados([FromBody] BaseRequestDTO request)
    {
        var (success, message, dados) = await _historicoService.ObterMateriaisReciclados(request.Token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = dados });
    }

    [HttpPost("reciclagem-ultimos-6-meses")]
    public async Task<IActionResult> ObterReciclagemUltimos6Meses([FromBody] BaseRequestDTO request)
    {
        var (success, message, dados) = await _historicoService.ObterReciclagemUltimos6Meses(request.Token);
        if (!success)
        {
            return BadRequest(new { status = false, message });
        }
        return Ok(new { status = true, message, data = dados });
    }

    /// <summary>
    /// Obtém o histórico de reciclagem do usuário com base nos filtros fornecidos.
    /// Todos os filtros são opcionais.
    /// </summary>
    /// <param name="request">
    ///   Token: Token JWT do usuário (obrigatório)
    ///   MaterialId: Filtro por ID do material (opcional)
    ///   EcopontoId: Filtro por ID do ecoponto (opcional)
    ///   DataInicio: Filtro por data inicial (opcional)
    ///   DataFim: Filtro por data final (opcional)
    ///   Pagina: Número da página a ser exibida - começa em 1 (opcional, padrão: 1)
    ///   Limite: Quantidade de registros por página (opcional, padrão: 10)
    /// </param>
    [HttpPost("reciclagem")]
    public async Task<IActionResult> ObterHistoricoReciclagem([FromBody] HistoricoReciclagemRequestDTO request)
    {
        var (success, message, reciclagens) = await _historicoService.ObterHistoricoReciclagem(
            request.Token,
            request.MaterialId,
            request.EcopontoId,
            request.DataInicio,
            request.DataFim,
            request.Pagina,
            request.Limite);

        if (!success)
        {
            return BadRequest(new { status = false, message });
        }

        return Ok(new
        {
            status = true,
            message,
            data = reciclagens,
            paginacao = new
            {
                pagina = request.Pagina,
                limite = request.Limite,
                total = reciclagens?.Count ?? 0
            }
        });
    }
}
