using System;

namespace EcoIpil.API.DTOs;

public class TicketResponseDTO
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TipoOperacao { get; set; }
    public string? Descricao { get; set; }
    public string? Status { get; set; }
    public DateTime? DataValidade { get; set; }
    public float Saldo { get; set; } // CORREÇÃO: decimal para float
    public string? TicketCode { get; set; }
}