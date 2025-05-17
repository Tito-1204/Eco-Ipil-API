using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

/// <summary>
/// DTO para processar um código QR e registrar reciclagem automaticamente.
/// O código QR deve conter o ID do agente, ID do ecoponto e ID do material no formato: "agente_id:ecoponto_id:material_id[:peso]"
/// </summary>
public class EscanearQRRequestDTO : BaseRequestDTO
{
    /// <summary>
    /// Código QR decodificado em formato de texto (String)
    /// </summary>
    [Required(ErrorMessage = "O código QR é obrigatório")]
    public string CodigoQR { get; set; } = string.Empty;
} 