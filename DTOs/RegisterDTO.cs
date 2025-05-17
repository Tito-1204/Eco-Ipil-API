using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class RegisterDTO
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(255, ErrorMessage = "O nome deve ter no máximo 255 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(255, ErrorMessage = "O email deve ter no máximo 255 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 255 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório")]
    [StringLength(255, ErrorMessage = "O telefone deve ter no máximo 255 caracteres")]
    public string Telefone { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "O gênero deve ter no máximo 255 caracteres")]
    public string? Genero { get; set; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateTime DataNascimento { get; set; }
} 