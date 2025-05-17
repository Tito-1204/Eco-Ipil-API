using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("investir")]
public class Investir : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("pontos_investidos")]
    public long PontosInvestidos { get; set; }

    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("investimento_id")]
    public long InvestimentoId { get; set; }

    [Column("data_retorno")]
    public DateTime DataRetorno { get; set; }

    [Column("valor_retorno")]
    public double ValorRetorno { get; set; }
}