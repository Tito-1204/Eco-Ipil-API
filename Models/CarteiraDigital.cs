using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("carteira_digital")]
public class CarteiraDigital : BaseModel
{
    [PrimaryKey("id")]
    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("pontos")]
    public long Pontos { get; set; }
    
    [Column("saldo")]
    public decimal Saldo { get; set; }
    
    [Column("usuario_id")]
    public long UsuarioId { get; set; }
} 