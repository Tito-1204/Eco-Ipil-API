using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;

namespace EcoIpil.API.Models
{
    [Table("notificacoes_lidas")]
    public class NotificacaoLida : BaseModel
    {
        [PrimaryKey("id", shouldInsert: false)]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [Column("usuario_id")]
        [JsonPropertyName("usuario_id")]
        public long? UsuarioId { get; set; }

        [Column("notificacao_id")]
        [JsonPropertyName("notificacao_id")]
        public long NotificacaoId { get; set; }

        [Column("data_leitura")]
        [JsonPropertyName("data_leitura")]
        public DateTime DataLeitura { get; set; } = DateTime.UtcNow;

        [Column("agente_id")]
        [JsonPropertyName("agente_id")]
        public long? AgenteId { get; set; }
    }
}