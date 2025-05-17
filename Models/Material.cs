using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("materiais")]
public class Material : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("classe")]
    public string? Classe { get; set; }

    [Column("valor")]
    public long Valor { get; set; }
} 