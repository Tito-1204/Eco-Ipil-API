using System.Collections.Generic;

namespace EcoIpil.API.DTOs;

public class HistoricoGraficoDTO
{
    public List<string> Labels { get; set; } = new List<string>();
    public List<float> Dados { get; set; } = new List<float>();
} 