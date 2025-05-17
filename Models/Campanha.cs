using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models
{
    [Table("campanhas")]
    public class Campanha : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = "Ativo";

        [Column("data_inicio")]
        public DateTime DataInicio { get; set; }

        [Column("data_fim")]
        public DateTime DataFim { get; set; }

        [Column("pontos")]
        public long Pontos { get; set; }
    }
} 