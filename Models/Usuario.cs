using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;



namespace EcoIpil.API.Models;

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id")]
    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("nome")]
    [Required]
    [StringLength(255)]
    public string Nome { get; set; } = string.Empty;
    
    [Column("email")]
    [Required]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Column("senha")]
    [Required]
    [StringLength(255)]
    public string Senha { get; set; } = string.Empty;
    
    [Column("telefone")]
    [Required]
    [StringLength(255)]
    public string Telefone { get; set; } = string.Empty;
    
    [Column("genero")]
    [StringLength(255)]
    public string? Genero { get; set; }
    
    [Column("localizacao")]
    [StringLength(255)]
    public string? Localizacao { get; set; }
    
    [Column("foto")]
    [StringLength(255)]
    public string? Foto { get; set; }
    
    [Column("pontos_totais")]
    public long PontosTotais { get; set; }
    
    [Column("data_nascimento")]
    [Required]
    public DateTime? DataNascimento { get; set; }

    [JsonPropertyName("preferencias")]
    [Column("preferencias")]
    public Dictionary<string, bool> Preferencias { get; set; } = new Dictionary<string, bool>
    {
        { "notificacoes_app", true },
        { "notificacoes_email", false },
        { "notificacoes_whatsapp", false },
        { "notificacoes_sms", false }
    };
    
    [Column("status")]
    [StringLength(255)]
    public string Status { get; set; } = "Ativo";
    
    [Column("ultimo_login")]
    public DateTime? UltimoLogin { get; set; }
    
    [Column("user_uid")]
    public string UserUid { get; set; } = string.Empty;

    [Column("last_personal_data_update")]
    public DateTime? LastPersonalDataUpdate { get; set; }
    
    // Método para ignorar o Id durante a atualização
    public Dictionary<string, object> GetUpdateDictionary()
    {
        return new Dictionary<string, object>
        {
            { "nome", Nome },
            { "email", Email },
            { "telefone", Telefone },
            { "genero", Genero ?? string.Empty },
            { "localizacao", Localizacao ?? string.Empty },
            { "foto", Foto ?? string.Empty },
            { "status", Status }
        };
    }
} 