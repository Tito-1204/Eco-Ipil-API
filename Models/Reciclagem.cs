using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("reciclagem")]
public class Reciclagem : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("peso")]
    public float Peso { get; set; }

    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("agente_id")]
    public long? AgenteId { get; set; }

    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("ecoponto_id")]
    public long EcopontoId { get; set; }

    [Column("coleta_id")]
    public long? ColetaId { get; set; }

} 