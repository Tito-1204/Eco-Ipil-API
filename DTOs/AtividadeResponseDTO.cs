namespace EcoIpil.API.DTOs;

public class AtividadeResponseDTO
{
    public string Tipo { get; set; } = null!; // "reciclagem", "recompensa", "conquista"
    public DateTime Timestamp { get; set; }
    public double? Peso { get; set; } // Para reciclagem
    public string? EcopontoNome { get; set; } // Para reciclagem
    public string? RecompensaNome { get; set; } // Para recompensa
    public long? PontosRecompensa { get; set; } // Para recompensa
    public string? ConquistaNome { get; set; } // Para conquista
    public long? PontosConquista { get; set; } // Para conquista
}