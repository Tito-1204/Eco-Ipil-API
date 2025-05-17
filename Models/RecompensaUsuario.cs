using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("recompensas_usuarios")]
public class RecompensaUsuario : BaseModel
{
    [PrimaryKey("recompensa_id", false)]
    [Column("recompensa_id")]
    public long RecompensaId { get; set; }

    [PrimaryKey("usuario_id", false)]
    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("data_recompensa")]
    public DateTime DataRecompensa { get; set; }

    public Recompensa? Recompensa { get; set; }
} 