using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("notificacoes_lidas")]
public class NotificacaoLida : BaseModel
{
    [PrimaryKey("usuario_id", false)]
    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [PrimaryKey("notificacao_id", false)]
    [Column("notificacao_id")]
    public long NotificacaoId { get; set; }

    [Column("data_leitura")]
    public DateTime DataLeitura { get; set; }
}