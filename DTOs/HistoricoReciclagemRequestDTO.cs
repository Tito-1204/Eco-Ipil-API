using System;

namespace EcoIpil.API.DTOs;

public class HistoricoReciclagemRequestDTO : BaseRequestDTO
{
    public long? MaterialId { get; set; } = null;
    public long? EcopontoId { get; set; } = null;
    public DateTime? DataInicio { get; set; } = null;
    public DateTime? DataFim { get; set; } = null;
    public int Pagina { get; set; } = 1;
    public int Limite { get; set; } = 10;
} 