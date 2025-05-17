using System;
using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("conquistas_usuarios")]
public class ConquistasUsuarios : BaseModel
{
    [Column("data_conquista")]
    public DateTime DataConquista { get; set; }

    [PrimaryKey("conquista_id", true)]
    [Column("conquista_id")]
    public long ConquistaId { get; set; }

    [PrimaryKey("usuario_id", true)]
    [Column("usuario_id")]
    public long UsuarioId { get; set; }
    
    [Reference(typeof(Conquista))]
    public Conquista Conquista { get; set; } = null!;
}