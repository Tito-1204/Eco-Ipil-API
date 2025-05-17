namespace EcoIpil.API.DTOs
{

    public class ReciclagemMensalDTO
    {
        public required string Mes { get; set; } // Exemplo: "Jan/2025"
        public float TotalPeso { get; set; }
    }
}