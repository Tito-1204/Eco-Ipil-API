using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs
{
    public class PreferenciasDTO
{
    public bool? NotificacoesApp { get; set; }
    public bool? NotificacoesEmail { get; set; }
}

    public class AtualizarConfiguracaoRequestDTO
    {
        [Required(ErrorMessage = "O token é obrigatório")]
        public string Token { get; set; } = string.Empty;

        public string? Nome { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Localizacao { get; set; }
        public PreferenciasDTO? Preferencias { get; set; }
        public string? Senha { get; set; }
    }
}