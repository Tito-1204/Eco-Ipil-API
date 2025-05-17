using System;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models
{
    [Table("usuarios_campanhas")]
    public class UsuarioCampanha : BaseModel
    {
        [PrimaryKey("usuario_id", false)]
        [Column("usuario_id")]
        public long UsuarioId { get; set; }

        [PrimaryKey("campanha_id", false)]
        [Column("campanha_id")]
        public long CampanhaId { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pendente";

        [JsonIgnore]
        public DateTime? CreatedAt { get; set; }
    }
}
