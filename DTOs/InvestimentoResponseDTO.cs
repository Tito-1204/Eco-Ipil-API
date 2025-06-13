namespace EcoIpil.API.DTOs;

public class InvestimentoResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long TotalInvestido { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public long Meta { get; set; }
    public string Status { get; set; } = "Ativo";
    public string Descricao { get; set; } = string.Empty; // Apenas dados da tabela investimentos
}

public class UserInvestmentResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long TotalInvestido { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public long Meta { get; set; }
    public string Status { get; set; } = "Ativo";
    public long? PontosInvestidos { get; set; }
    public DateTime? DataRetorno { get; set; }
    public long? ValorRetorno { get; set; }
    public string Descricao { get; set; } = string.Empty; // Dados combinados de investimentos e investir
}