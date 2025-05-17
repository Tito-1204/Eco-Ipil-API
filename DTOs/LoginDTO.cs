using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class LoginDTO
{
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;

    public bool ManterConectado { get; set; }
}