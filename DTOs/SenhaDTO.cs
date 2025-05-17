using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class SenhaDTO : BaseRequestDTO
{
    [Required(ErrorMessage = "A senha atual é obrigatória")]
    [StringLength(255, ErrorMessage = "A senha atual deve ter no máximo 255 caracteres")]
    public string SenhaAtual { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A nova senha é obrigatória")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "A nova senha deve ter entre 6 e 255 caracteres")]
    public string NovaSenha { get; set; } = string.Empty;
} 