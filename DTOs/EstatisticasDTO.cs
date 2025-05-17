namespace EcoIpil.API.DTOs
{
    public class EstatisticasDTO
    {
        public int Reciclagens { get; set; }
        public int Conquistas { get; set; }
    }

    public class PerfilUsuarioDTO
    {
        public long PontosTotais { get; set; }
        public float TotalReciclado { get; set; }
        public float CO2Evitado { get; set; }
        public required EstatisticasDTO Estatisticas { get; set; }
        public int Nivel { get; set; }
    }
}