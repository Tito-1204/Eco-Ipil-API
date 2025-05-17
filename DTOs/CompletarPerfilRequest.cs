using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;
public class CompletarPerfilRequest
{
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateTime? DataNascimento { get; set; }

    [Required(ErrorMessage = "O gênero é obrigatório")]
    [RegularExpression("^(Masculino|Feminino|Outro)$", ErrorMessage = "O gênero deve ser 'Masculino', 'Feminino' ou 'Outro'")]
    public string Genero { get; set; } = string.Empty;
}