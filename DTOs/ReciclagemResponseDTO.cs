namespace EcoIpil.API.DTOs;

public class ReciclagemResponseDTO
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public float Peso { get; set; }
    public long UsuarioId { get; set; }
    public long MaterialId { get; set; }
    public long EcopontoId { get; set; }
    public long? AgenteId { get; set; }
    public string? MaterialNome { get; set; }
    public string? MaterialClasse { get; set; }
    public long PontosGanhos { get; set; }
    public string? EcopontoNome { get; set; }
    public string? EcopontoLocalizacao { get; set; }
    public string? AgenteNome { get; set; }
}