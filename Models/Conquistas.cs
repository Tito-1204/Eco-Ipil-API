using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("conquistas")]
public class Conquista : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("nome")]
    [Required]
    [StringLength(255)]
    public string Nome { get; set; } = string.Empty;

    [Column("descricao")]
    [StringLength(255)]
    public string? Descricao { get; set; }

    [Column("pontos")]
    public long Pontos { get; set; }
}