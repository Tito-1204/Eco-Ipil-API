namespace EcoIpil.API.DTOs;

public class ReciclagemRequestDTO
{
    public required string Token { get; set; }
    public long MaterialId { get; set; }
    public float Peso { get; set; }
    public long EcopontoId { get; set; }
    public string? Qualidade { get; set; }
    public long? AgenteId { get; set; }
}