using System;

namespace EcoIpil.API.DTOs;

/// <summary>
/// DTO de resposta para o resgate de uma recompensa
/// </summary>
public class RecompensaResgateResponseDTO
{
    /// <summary>
    /// ID da recompensa
    /// </summary>
    public long RecompensaId { get; set; }

    /// <summary>
    /// Nome da recompensa
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da recompensa (ex: "Desconto", "Produto", "Experiência")
    /// </summary>
    public string? Tipo { get; set; }

    /// <summary>
    /// Descrição detalhada da recompensa
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Valor em pontos da recompensa
    /// </summary>
    public long Pontos { get; set; }

    /// <summary>
    /// Data em que o resgate foi solicitado
    /// </summary>
    public DateTime DataResgate { get; set; }

    /// <summary>
    /// Status do resgate (ex: "Pendente", "Resgatada", "Expirado")
    /// </summary>
    public string Status { get; set; } = "Pendente";

    /// <summary>
    /// Código do ticket associado ao resgate
    /// </summary>
    public string? TicketCode { get; set; }
}