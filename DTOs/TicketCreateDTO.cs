namespace EcoIpil.API.DTOs;

public class TicketCreateDTO
{
    public string Token { get; set; } = string.Empty;
    public string TipoOperacao { get; set; } = string.Empty; // "LevantamentoExpress" ou "PagamentoMao"
    public string? Descricao { get; set; }
    public decimal Valor { get; set; }
}