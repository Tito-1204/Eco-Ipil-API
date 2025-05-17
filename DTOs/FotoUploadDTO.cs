using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoIpil.API.DTOs;

public class FotoUploadDTO
{
    [Required(ErrorMessage = "A foto é obrigatória")]
    public IFormFile Foto { get; set; } = null!;

    [Required(ErrorMessage = "O UID do usuário é obrigatório")]
    public string UserUid { get; set; } = string.Empty;
} 