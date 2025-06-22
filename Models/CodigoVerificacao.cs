using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace EcoIpil.API.Models
{
    [Table("codigos_verificacao")]
    public class CodigoVerificacao : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("codigo")]
        public string Codigo { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; }

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; }

        [Column("expira_em")]
        public DateTime ExpiraEm { get; set; }
    }
}