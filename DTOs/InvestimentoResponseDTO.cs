namespace EcoIpil.API.DTOs;

public class InvestimentoResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long TotalInvestido { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public long Meta { get; set; }
    public string Status { get; set; } = string.Empty;
    public long PontosInvestidos { get; set; } // Adicionado
    public DateTime DataRetorno { get; set; } // Adicionado
    public long ValorRetorno { get; set; } // Adicionado
}