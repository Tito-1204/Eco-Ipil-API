using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class PerfilDTO : BaseRequestDTO
{
    [StringLength(255, ErrorMessage = "O nome deve ter no máximo 255 caracteres")]
    public string? Nome { get; set; }
    
    [StringLength(255, ErrorMessage = "O telefone deve ter no máximo 255 caracteres")]
    public string? Telefone { get; set; }
    
    [StringLength(255, ErrorMessage = "O gênero deve ter no máximo 255 caracteres")]
    public string? Genero { get; set; }
    
    [StringLength(255, ErrorMessage = "A localização deve ter no máximo 255 caracteres")]
    public string? Localizacao { get; set; }
} 