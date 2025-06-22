using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        var (success, message, token) = await _authService.Login(loginDto);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message,
            data = new { token }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
    {
        var (success, message) = await _authService.Register(registerDto);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message
        });
    }

    [HttpPost("send-welcome-email")]
    public async Task<IActionResult> SendWelcomeEmail([FromBody] WelcomeEmailDTO welcomeEmailDto)
    {
        var (success, message) = await _authService.SendWelcomeEmail(welcomeEmailDto.Email, welcomeEmailDto.Nome);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message
        });
    }

    [HttpPost("verify-email-code")]
    public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeDTO verifyEmailCodeDto)
    {
        var (success, message) = await _authService.VerifyEmailCode(verifyEmailCodeDto.Email, verifyEmailCodeDto.Codigo);

        if (!success)
            return BadRequest(new { status = false, message });

        return Ok(new
        {
            status = true,
            message
        });
    }
}

public class WelcomeEmailDTO
{
    public string Email { get; set; }
    public string Nome { get; set; }
}

public class VerifyEmailCodeDTO
{
    public string Email { get; set; }
    public string Codigo { get; set; }
}