using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.Models;

[Table("ecopontos")]
public class Ecoponto : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("nome")]
    [Required]
    [StringLength(255)]
    public string Nome { get; set; } = string.Empty;
    
    [Column("localizacao")]
    [StringLength(255)]
    public string? Localizacao { get; set; } // Pode ser mantido para compatibilidade ou dados legados
    
    [Column("status")]
    [StringLength(255)]
    public string Status { get; set; } = "Ativo";
    
    [Column("capacidade")]
    public float Capacidade { get; set; }
    
    [Column("preenchido_atual")]
    public float PreenchidoAtual { get; set; }
    
    [Column("sensor_peso")]
    public float SensorPeso { get; set; }
    
    [Column("sensor_tipo")]
    [StringLength(255)]
    public string? SensorTipo { get; set; }
    
    [Column("sensor_termico")]
    public float SensorTermico { get; set; }
    
    [Column("sensor_status")]
    [StringLength(255)]
    public string? SensorStatus { get; set; }
    
    [Column("material_suportado")]
    [StringLength(255)]
    public string? MaterialSuportado { get; set; }

    [Column("latitude")]
    public float? Latitude { get; set; } // SQL numeric(10,8) pode ser float ou double. Float é usado no DTO.

    [Column("longitude")]
    public float? Longitude { get; set; } // SQL numeric(11,8) pode ser float ou double.

    [Column("agente_responsavel_id")]
    public long? AgenteResponsavelId { get; set; }
}