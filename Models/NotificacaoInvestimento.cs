using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;
namespace EcoIpil.API.Models;

public class NotificacaoInvestimento : BaseModel
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public long InvestimentoId { get; set; }
    public string? Tipo { get; set; }
    public DateTime DataEnvio { get; set; }
}