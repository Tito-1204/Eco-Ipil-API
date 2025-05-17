using System;

namespace EcoIpil.API.DTOs;

/// <summary>
/// DTO de resposta para recompensas resgatadas pelo usuário
/// </summary>
public class RecompensaUsuarioResponseDTO
{
    /// <summary>
    /// ID da recompensa
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nome da recompensa
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da recompensa (ex: "Desconto", "Produto", "Serviço")
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
    /// Data em que a recompensa foi resgatada
    /// </summary>
    public DateTime DataResgate { get; set; }

    /// <summary>
    /// Status da recompensa (ex: "Resgatada", "Utilizada")
    /// </summary>
    public string Status { get; set; } = "Resgatada";
} 