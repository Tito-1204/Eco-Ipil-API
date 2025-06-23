using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EcoIpil.API.Models
{
    [Table("notificacoes")]
    public class Notificacao : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("mensagem")]
        [JsonPropertyName("mensagem")]
        public string Mensagem { get; set; } = string.Empty;

        [Column("tipo")]
        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [Column("lidos")]
        [JsonPropertyName("lidos")]
        public long Lidos { get; set; }

        [Column("data_expiracao")]
        [JsonPropertyName("data_expiracao")]
        public DateTime? DataExpiracao { get; set; }

        [Column("usuario_id")]
        [JsonPropertyName("usuario_id")]
        public long? UsuarioId { get; set; }

        [Reference(typeof(NotificacaoLida))]
        [JsonPropertyName("notificacoes_lidas")]
        public List<NotificacaoLida> NotificacoesLidas { get; set; } = new List<NotificacaoLida>();
    }
}