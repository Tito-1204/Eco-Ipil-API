using System;

namespace EcoIpil.API.DTOs;

/// <summary>
/// DTO de resposta para informações de recompensa
/// </summary>
public class RecompensaResponseDTO
{
    /// <summary>
    /// ID da recompensa
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Data de criação da recompensa
    /// </summary>
    public DateTime CreatedAt { get; set; }

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
    /// Quantidade de pontos necessários para resgate
    /// </summary>
    public long Pontos { get; set; }

    /// <summary>
    /// Quantidade disponível em estoque
    /// </summary>
    public long QtRestante { get; set; }

    /// <summary>
    /// Indica se a recompensa está disponível para resgate
    /// </summary>
    public bool Disponivel => QtRestante > 0;
} 