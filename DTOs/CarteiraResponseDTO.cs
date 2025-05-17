namespace EcoIpil.API.DTOs;

public class CarteiraResponseDTO
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Pontos { get; set; }
    public decimal Saldo { get; set; }
    public long UsuarioId { get; set; }
} 