using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Models;

[Table("agentes")]
public class Agente : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("senha")]
    public string Senha { get; set; } = string.Empty;

    [Column("telefone")]
    public string Telefone { get; set; } = string.Empty;

    [Column("genero")]
    public string? Genero { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Ativo";

    [Column("area")]
    public string Area { get; set; } = string.Empty;

    [Column("pontos_desempenho")]
    public long PontosDesempenho { get; set; }

    [Column("departamento_id")]
    public long? DepartamentoId { get; set; }

    [Column("candidato_id")]
    public long? CandidatoId { get; set; }
} 