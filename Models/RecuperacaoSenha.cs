using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models
{
    [Table("recuperacao_senha")]
    public class RecuperacaoSenha : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}