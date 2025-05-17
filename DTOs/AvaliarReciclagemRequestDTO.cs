namespace EcoIpil.API.DTOs;

public class AvaliarReciclagemRequestDTO
{
    public required string Token { get; set; }
    public int Rating { get; set; }
    public string? Comentario { get; set; }
}