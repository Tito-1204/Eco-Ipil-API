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
        [FromQuery] float? latitude = null, // Usado como centro para busca por raio
        [FromQuery] float? longitude = null, // Usado como centro para busca por raio
        [FromQuery] float? raio = null,
        [FromQuery] string? material = null,
        [FromQuery] string? status = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 10)
    {
        // Os parâmetros latitude, longitude e raio são usados para a busca por proximidade.
        // O EcopontoService foi atualizado para usar as colunas 'latitude' e 'longitude' do banco
        // para esse cálculo de distância, em vez de depender apenas da string 'localizacao'.
        var (success, message, ecopontos) = await _ecopontoService.ListarEcopontos(
            latitude, longitude, raio, material, status, pagina, limite);

        if (!success)
            // Considerar retornar um status code mais apropriado dependendo do erro, ex: 500 para erro interno
            return BadRequest(new { status = false, message });

        // O total na paginação agora reflete o número de itens *após* a filtragem (incluindo distância) e paginação.
        // Se você precisar do total *antes* da paginação manual feita após o filtro de distância,
        // o serviço precisaria retornar essa contagem separadamente.
        // Por simplicidade, o total aqui é o número de itens na página atual.
        return Ok(new
        {
            status = true,
            message,
            data = ecopontos,
            paginacao = new
            {
                pagina,
                limite,
                total_nesta_pagina = ecopontos?.Count ?? 0 // Modificado para clareza
                // Para um 'total_geral_filtrado', o serviço precisaria calcular e retornar isso.
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterEcoponto([FromRoute] long id)
    {
        var (success, message, ecoponto) = await _ecopontoService.ObterEcoponto(id);

        if (!success)
        {
            if (message.Contains("não encontrado")) // Melhorar a detecção de "não encontrado"
                return NotFound(new { status = false, message });
            return BadRequest(new { status = false, message });
        }

        return Ok(new
        {
            status = true,
            message,
            data = ecoponto
        });
    }
}