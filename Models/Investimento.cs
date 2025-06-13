using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("investimentos")]
public class Investimento : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("total_investido")]
    public long TotalInvestido { get; set; }

    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("meta")]
    public double Meta { get; set; } // Alterado de long para double

    [Column("status")]
    public string Status { get; set; } = "Ativo";

    [Column("descricao")]
    public string Descricao { get; set; } = string.Empty;
}