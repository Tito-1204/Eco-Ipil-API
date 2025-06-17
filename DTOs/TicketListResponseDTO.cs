using System.Collections.Generic;

namespace EcoIpil.API.DTOs;

public class TicketListResponseDTO
{
    public List<TicketResponseDTO> Tickets { get; set; } = new List<TicketResponseDTO>();
    public PaginationMeta Meta { get; set; } = new PaginationMeta();
}

public class PaginationMeta
{
    public long Total { get; set; }
    public int Pagina { get; set; }
    public int Limite { get; set; }
    public int Paginas { get; set; }
}