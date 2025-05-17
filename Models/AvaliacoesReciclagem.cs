using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("avaliacoes_reciclagem")]
public class AvaliacoesReciclagem : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("reciclagem_id")]
    public long ReciclagemId { get; set; }

    [Column("usuario_id")]
    public long? UsuarioId { get; set; }

    [Column("agente_id")]
    public long AgenteId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("comentario")]
    public string? Comentario { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}