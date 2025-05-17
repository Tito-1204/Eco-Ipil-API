namespace EcoIpil.API.DTOs;

public class GraficoRequestDTO : BaseRequestDTO
{
    public string Periodo { get; set; } = "mensal";
    public int? Ano { get; set; }
    public int? Mes { get; set; }
} 