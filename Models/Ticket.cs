using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;

namespace EcoIpil.API.Models;

[Table("tickets")]
public class Ticket : BaseModel
{
    [PrimaryKey("id", shouldInsert: false)]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("tipo_operacao")]
    public string? TipoOperacao { get; set; }

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("data_validade")]
    public DateTime? DataValidade { get; set; }

    // CORREÇÃO: Alinhando com a nulidade da tabela (real null)
    [Column("saldo")]
    public float? Saldo { get; set; } 

    [Column("ticket_code")]
    public string? TicketCode { get; set; }

    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("agente_id")]
    public long? AgenteId { get; set; }
}