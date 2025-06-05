using System;

namespace EcoIpil.API.DTOs;

/// <summary>
/// DTO de resposta para recompensas resgatadas pelo usuário
/// </summary>
public class RecompensaUsuarioResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public long Pontos { get; set; }
    public DateTime DataResgate { get; set; }
    public string Status { get; set; } = "Pendente";
    public string? TicketCode { get; set; }
}