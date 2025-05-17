using System.ComponentModel.DataAnnotations;

namespace EcoIpil.API.DTOs;

public class TransferenciaDTO : BaseRequestDTO
{
    [Required(ErrorMessage = "O ID do destinatário é obrigatório")]
    public required string UidDestinatario { get; set; }

    [Required(ErrorMessage = "O valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "O tipo de transferência é obrigatório")]
    [RegularExpression("^(pontos|saldo)$", ErrorMessage = "O tipo deve ser 'pontos' ou 'saldo'")]
    public string Tipo { get; set; } = string.Empty;
} 