using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class BaseRequestDTO
{
    [Required(ErrorMessage = "O token é obrigatório")]
    public string Token { get; set; } = string.Empty;
} 