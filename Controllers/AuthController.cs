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
} 