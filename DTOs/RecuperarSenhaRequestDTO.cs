namespace EcoIpil.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class RecuperarSenhaRequestDTO
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "O formato do email é inválido")]
        public string? Email { get; set; }
    }
}