using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs
{
    public class ConfiguracaoResponseDTO
    {
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Localizacao { get; set; }
        public object Preferencias { get; set; } = new { notificacoes_app = true, notificacoes_email = false, notificacoes_whatsapp = false, notificacoes_sms = false };
    }

}