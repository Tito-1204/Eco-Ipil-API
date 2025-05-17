using System;

namespace EcoIpil.API.DTOs;

public class MaterialResponseDTO
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Classe { get; set; }
    public long Valor { get; set; }
    // Campos adicionais para informações mais detalhadas do material
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public string? Dicas { get; set; }
} 