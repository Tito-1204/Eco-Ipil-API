using System;

namespace EcoIpil.API.DTOs;

public class ConquistaUsuarioResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long Pontos { get; set; }
    public DateTime DataConquista { get; set; }
}