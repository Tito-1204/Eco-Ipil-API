using System;

namespace EcoIpil.API.DTOs;

public class PerfilResponseDTO
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string? Localizacao { get; set; }
    public string? Foto { get; set; }
    public long PontosTotais { get; set; }
    public DateTime DataNascimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? UltimoLogin { get; set; }
    public string UserUid { get; set; } = string.Empty;
} 