using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("trocapontos")]
public class TrocaPontos : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("pontos_trocados")]
    public long PontosTrocados { get; set; }

    [Column("saldo_obtido")]
    public decimal SaldoObtido { get; set; }

    [Column("data_troca")]
    public DateTime DataTroca { get; set; }
}