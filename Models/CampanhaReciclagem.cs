using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models
{
    [Table("campanhas_reciclagem")]
    public class CampanhaReciclagem : BaseModel
    {
        [PrimaryKey("campanha_id", false)]
        public long CampanhaId { get; set; }

        [PrimaryKey("reciclagem_id", false)]
        public long ReciclagemId { get; set; }
    }
} 