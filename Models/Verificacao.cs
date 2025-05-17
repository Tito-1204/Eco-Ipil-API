using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models
{
    [Table("verificacoes")]
    public class Verificacao : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("token")]
        public string? Token { get; set; }

        [Column("telefone")]
        public string? Telefone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; } // Adicionada a propriedade ExpiresAt
    }
}