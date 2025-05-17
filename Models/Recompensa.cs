using System;
using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("recompensas")]
public class Recompensa : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("nome")]
    [Required]
    [MaxLength(255)]
    public string Nome { get; set; } = string.Empty;

    [Column("tipo")]
    public string? Tipo { get; set; }

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Column("pontos")]
    public long Pontos { get; set; } = 0;

    [Column("qt_restante")]
    public long QtRestante { get; set; } = 0;
} 