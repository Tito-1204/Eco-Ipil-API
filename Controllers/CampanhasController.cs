using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcoIpil.API.Services;
using EcoIpil.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace EcoIpil.API.Controllers
{
    [ApiController]
    [Route("api/v1/campanhas")]
    public class CampanhasController : ControllerBase
    {
        private readonly CampanhaService _campanhaService;
        private readonly UsuarioService _usuarioService;
        private readonly ILogger<CampanhasController> _logger;

        public CampanhasController(CampanhaService campanhaService, UsuarioService usuarioService, ILogger<CampanhasController> logger)
        {
            _campanhaService = campanhaService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas as campanhas ativas.
        /// O token JWT é opcional e deve ser enviado como parâmetro de query "token".
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarCampanhas([FromQuery] string token)
        {
            try
            {
                long? usuarioId = null;
                if (!string.IsNullOrEmpty(token))
                {
                    var (success, message, userId) = await _usuarioService.ValidateToken(token);
                    if (success)
                    {
                        usuarioId = userId;
                    }
                }
                
                var resultado = await _campanhaService.ListarCampanhasAtivas(usuarioId);
                
                if (resultado.Success)
                {
                    return Ok(new { status = true, message = resultado.Message, data = resultado.Campanhas });
                }
                
                return BadRequest(new { status = false, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar campanhas");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Obtém detalhes de uma campanha específica.
        /// Não exige token JWT no header.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterCampanha(long id)
        {
            try
            {
                // Chama o serviço sem userId, pois o token não é requerido neste endpoint
                var resultado = await _campanhaService.ObterCampanha(id, null);
                
                if (resultado.Success)
                {
                    return Ok(new { status = true, message = resultado.Message, data = resultado.Campanha });
                }
                
                return BadRequest(new { status = false, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter campanha ID: {id}");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Registra a participação do usuário em uma campanha.
        /// Exige o token JWT no corpo da requisição.
        /// </summary>
        [HttpPost("{id}/participar")]
        public async Task<IActionResult> ParticiparCampanha(long id, [FromBody] TokenRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { status = false, message = "Token JWT é obrigatório" });
                }
                
                // Valida o token e obtém o userId usando o UsuarioService
                var (success, message, userId) = await _usuarioService.ValidateToken(request.Token);
                if (!success)
                {
                    return BadRequest(new { status = false, message });
                }
                
                var resultado = await _campanhaService.ParticiparCampanha(id, userId);
                
                if (resultado.Success)
                {
                    return Ok(new { status = true, message = resultado.Message });
                }
                
                return BadRequest(new { status = false, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao participar da campanha ID: {id}");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Endpoint administrativo para verificar o cumprimento de uma campanha por um usuário.
        /// </summary>
        [HttpPost("{id}/verificar-cumprimento")]
        [Authorize(Roles = "Administrador,Agente")]
        public async Task<IActionResult> VerificarCumprimentoCampanha(long id, [FromBody] ParticipacaoCampanhaRequestDTO request)
        {
            try
            {
                var resultado = await _campanhaService.VerificarCumprimentoCampanha(request.UsuarioId, id);
                
                if (resultado.Success)
                {
                    return Ok(new { status = true, message = resultado.Message });
                }
                
                return BadRequest(new { status = false, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar cumprimento da campanha ID: {id}");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Endpoint administrativo para marcar uma reciclagem como parte de uma campanha.
        /// </summary>
        [HttpPost("{id}/registrar-reciclagem")]
        [Authorize(Roles = "Administrador,Agente")]
        public async Task<IActionResult> RegistrarReciclagemCampanha(long id, [FromBody] long reciclagemId)
        {
            try
            {
                var resultado = await _campanhaService.RegistrarReciclagemCampanha(reciclagemId, id);
                
                if (resultado.Success)
                {
                    return Ok(new { status = true, message = resultado.Message });
                }
                
                return BadRequest(new { status = false, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar reciclagem para campanha ID: {id}");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }
    }
}
