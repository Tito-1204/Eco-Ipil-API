using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs
{
    public class TokenRequestDTO
    {
        [Required(ErrorMessage = "Token JWT é obrigatório")]
        public string Token { get; set; } = string.Empty;
    }
} 