using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs
{
    public class ConfirmarTelefoneRequestDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve ter 6 dígitos")]
        public string Codigo { get; set; } = string.Empty;
    }
}