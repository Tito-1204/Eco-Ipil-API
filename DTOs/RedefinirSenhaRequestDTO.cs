namespace EcoIpil.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class RedefinirSenhaRequestDTO
    {
        [Required(ErrorMessage = "O código é obrigatório")]
        public required string Codigo { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
        public required string NovaSenha { get; set; }
    }
}