namespace EcoIpil.API.DTOs;

public class InvestirRequestDTO
{
    public long UserId { get; set; }
    public long InvestimentoId { get; set; }
    public long PontosInvestidos { get; set; }
    public required string Token { get; set; } // Adicionado para receber o token
}