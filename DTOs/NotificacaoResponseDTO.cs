namespace EcoIpil.API.DTOs;

public class NotificacaoResponseDTO
{
    public long Id { get; set; }
    public required string Mensagem { get; set; }
    public string? Tipo { get; set; }
    public int Lidos { get; set; }
    public DateTime? DataExpiracao { get; set; }
}