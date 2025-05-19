namespace EcoIpil.API.DTOs;

public class EcopontoResponseDTO
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Localizacao { get; set; } // Mantido por enquanto
    public float? Latitude { get; set; } // Nova propriedade
    public float? Longitude { get; set; } // Nova propriedade
    public string Status { get; set; } = string.Empty;
    public float Capacidade { get; set; }
    public float PreenchidoAtual { get; set; }
    public float SensorPeso { get; set; }
    public string? SensorTipo { get; set; }
    public float SensorTermico { get; set; }
    public string? SensorStatus { get; set; }
    public string? MaterialSuportado { get; set; }
    public string? NomeAgenteResponsavel { get; set; } // Nova propriedade
    
    // Campos adicionais para busca por proximidade
    public float? Distancia { get; set; }
}