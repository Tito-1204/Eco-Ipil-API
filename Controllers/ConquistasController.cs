using System;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EcoIpil.API.Controllers
{
    [ApiController]
    [Route("api/v1/conquistas")]
    public class ConquistasController : ControllerBase
    {
        private readonly ConquistasService _conquistasService;
        private readonly UsuarioService _usuarioService;
        private readonly ILogger<ConquistasController> _logger;

        public ConquistasController(
            ConquistasService conquistasService,
            UsuarioService usuarioService,
            ILogger<ConquistasController> logger)
        {
            _conquistasService = conquistasService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas as conquistas disponíveis no sistema.
        /// Não exige autenticação.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarConquistas()
        {
            try
            {
                var conquistas = await _conquistasService.GetAllConquistas();

                if (!conquistas.Any())
                {
                    return Ok(new { status = true, message = "Nenhuma conquista disponível no momento", data = new List<object>() });
                }

                var conquistasResponse = conquistas.Select(c => new
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    Pontos = c.Pontos
                }).ToList();

                return Ok(new { status = true, message = "Conquistas obtidas com sucesso", data = conquistasResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar conquistas");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Obtém as conquistas de um usuário específico.
        /// O token JWT deve ser enviado como parâmetro de query "token".
        /// </summary>
        [HttpGet("usuario")]
        public async Task<IActionResult> GetUserConquistas([FromQuery] string token)
        {
            try
            {
                // Verificar se o token foi fornecido
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { status = false, message = "O token é obrigatório" });
                }

                // Validar o token usando o UsuarioService
                var (success, message, userId) = await _usuarioService.ValidateToken(token);
                if (!success)
                {
                    return BadRequest(new { status = false, message });
                }

                // Obter as conquistas do usuário
                var conquistas = await _conquistasService.GetUserConquistas(userId);

                // Verificar se o usuário tem conquistas
                if (!conquistas.Any())
                {
                    return Ok(new { status = true, message = "Nenhuma conquista atribuída ao usuário", data = new List<object>() });
                }

                // Mapear os dados para um formato de resposta
                var conquistasResponse = conquistas.Select(cu => new
                {
                    Id = cu.Conquista.Id,
                    Nome = cu.Conquista.Nome,
                    Descricao = cu.Conquista.Descricao,
                    Pontos = cu.Conquista.Pontos,
                    DataConquista = cu.DataConquista
                }).ToList();

                return Ok(new { status = true, message = "Conquistas do usuário obtidas com sucesso", data = conquistasResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter conquistas do usuário");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }

        /// <summary>
        /// Verifica e atribui conquistas a um usuário.
        /// O token JWT deve ser enviado como parâmetro de query "token".
        /// </summary>
        [HttpPost("verificar")]
        public async Task<IActionResult> VerificarConquistas([FromQuery] string token)
        {
            try
            {
                // Verificar se o token foi fornecido
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { status = false, message = "O token é obrigatório" });
                }

                // Validar o token usando o UsuarioService
                var (success, message, userId) = await _usuarioService.ValidateToken(token);
                if (!success)
                {
                    return BadRequest(new { status = false, message });
                }

                // Verificar e atribuir conquistas
                await _conquistasService.CheckAndAssignAchievements(userId);

                return Ok(new { status = true, message = "Conquistas verificadas e atribuídas com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar conquistas do usuário");
                return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
            }
        }
    }
}