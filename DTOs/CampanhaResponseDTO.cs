using System;

namespace EcoIpil.API.DTOs
{
    public class CampanhaResponseDTO
    {
        public long Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public long Pontos { get; set; }
        public string ParticipacaoStatus { get; set; } = "Não Participando";
    }
} 