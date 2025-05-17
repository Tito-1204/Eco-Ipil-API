using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoIpil.API.Services;
using EcoIpil.API.DTOs;

namespace EcoIpil.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfiguracoesController : ControllerBase
    {
        private readonly ConfiguracaoService _configuracaoService;
        private readonly UsuarioService _usuarioService;

        public ConfiguracoesController(ConfiguracaoService configuracaoService, UsuarioService usuarioService)
        {
            _configuracaoService = configuracaoService;
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> GetConfiguracoes([FromQuery] string token)
        {
            var (success, message, userId) = await _usuarioService.ValidateToken(token);
            if (!success) return Unauthorized(new { status = false, message });

            var (successConfig, messageConfig, data) = await _configuracaoService.ObterConfiguracoes(userId);
            return successConfig ? Ok(new { status = true, data, message = messageConfig })
                                : BadRequest(new { status = false, message = messageConfig });
        }

        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> AtualizarConfiguracoes([FromBody] AtualizarConfiguracaoRequestDTO request)
        {
            var (success, message, userId) = await _usuarioService.ValidateToken(request.Token);
            if (!success) return Unauthorized(new { status = false, message });

            var (successUpdate, messageUpdate) = await _configuracaoService.AtualizarConfiguracoes(userId, request);
            return successUpdate ? Ok(new { status = true, message = messageUpdate })
                                : BadRequest(new { status = false, message = messageUpdate });
        }

        [HttpPost("confirmar-telefone")]
        public async Task<IActionResult> ConfirmarTelefone([FromBody] ConfirmarTelefoneRequestDTO request)
        {
            var (success, message, userId) = await _usuarioService.ValidateToken(request.Token);
            if (!success) return Unauthorized(new { status = false, message });

            var (successConfirm, messageConfirm) = await _configuracaoService.ConfirmarAlteracaoTelefone(userId, request.Codigo);
            return successConfirm ? Ok(new { status = true, message = messageConfirm })
                                 : BadRequest(new { status = false, message = messageConfirm });
        }

        [HttpGet("confirmar-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmarEmail([FromQuery] string token)
        {
            var (success, message) = await _configuracaoService.ConfirmarEmail(token);
            return success ? Ok(new { status = true, message })
                          : BadRequest(new { status = false, message });
        }

        [HttpGet("uid")]
        public async Task<IActionResult> ObterUid([FromQuery] string token)
        {
            var (success, message, userId) = await _usuarioService.ValidateToken(token);
            if (!success) return Unauthorized(new { status = false, message });

            var (successUid, messageUid, uid) = await _configuracaoService.ObterUid(userId);
            return successUid ? Ok(new { status = true, uid, message = messageUid })
                            : BadRequest(new { status = false, message = messageUid });
        }
    }
}