using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class EcopontoService
{
    private readonly Supabase.Client _supabaseClient;

    public EcopontoService(SupabaseService supabaseService)
    {
        _supabaseClient = supabaseService.GetClient();
    }

    public async Task<(bool success, string message, List<EcopontoResponseDTO>? ecopontos)> ListarEcopontos(
        float? latitude = null,
        float? longitude = null,
        float? raio = null,
        string? material = null,
        string? status = null,
        int pagina = 1,
        int limite = 10)
    {
        try
        {
            var query = _supabaseClient
                .From<Ecoponto>()
                .Select("*");

            // Aplicar filtros
            if (!string.IsNullOrEmpty(material))
            {
                query = query.Filter("material_suportado", Operator.ILike, $"%{material}%");
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Filter("status", Operator.Equals, status);
            }

            // Aplicar paginação (se não estiver filtrando por distância)
            if (!(latitude.HasValue && longitude.HasValue && raio.HasValue))
            {
                int offset = (pagina - 1) * limite;
                query = query.Range(offset, offset + limite - 1);
            }

            var response = await query.Get();
            var ecopontos = response.Models;

            if (ecopontos == null || !ecopontos.Any())
            {
                return (true, "Nenhum ecoponto encontrado", new List<EcopontoResponseDTO>());
            }

            var ecopontosDTO = ecopontos.Select(e => new EcopontoResponseDTO
            {
                Id = e.Id,
                CreatedAt = e.CreatedAt,
                Nome = e.Nome,
                Localizacao = e.Localizacao,
                Status = e.Status,
                Capacidade = e.Capacidade,
                PreenchidoAtual = e.PreenchidoAtual,
                SensorPeso = e.SensorPeso,
                SensorTipo = e.SensorTipo,
                SensorTermico = e.SensorTermico,
                SensorStatus = e.SensorStatus,
                MaterialSuportado = e.MaterialSuportado
            }).ToList();

            // Se fornecidas coordenadas e raio, calcular distância
            if (latitude.HasValue && longitude.HasValue && raio.HasValue)
            {
                ecopontosDTO = ecopontosDTO
                    .Select(e =>
                    {
                        if (e.Localizacao != null)
                        {
                            var coords = e.Localizacao.Split(',');
                            if (coords.Length == 2 && 
                                float.TryParse(coords[0], out float ecoLat) && 
                                float.TryParse(coords[1], out float ecoLon))
                            {
                                e.Distancia = CalcularDistancia(latitude.Value, longitude.Value, ecoLat, ecoLon);
                            }
                        }
                        return e;
                    })
                    .Where(e => e.Distancia.HasValue && e.Distancia <= raio.Value)
                    .OrderBy(e => e.Distancia)
                    .ToList();
                
                // Aplicar paginação após o cálculo de distância
                ecopontosDTO = ecopontosDTO
                    .Skip((pagina - 1) * limite)
                    .Take(limite)
                    .ToList();
            }

            return (true, "Ecopontos obtidos com sucesso", ecopontosDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar ecopontos: {ex.Message}");
            return (false, "Erro ao listar ecopontos", null);
        }
    }

    public async Task<(bool success, string message, EcopontoResponseDTO? ecoponto)> ObterEcoponto(long id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Ecoponto>()
                .Where(e => e.Id == id)
                .Single();

            if (response == null)
            {
                return (false, "Ecoponto não encontrado", null);
            }

            var ecopontoDTO = new EcopontoResponseDTO
            {
                Id = response.Id,
                CreatedAt = response.CreatedAt,
                Nome = response.Nome,
                Localizacao = response.Localizacao,
                Status = response.Status,
                Capacidade = response.Capacidade,
                PreenchidoAtual = response.PreenchidoAtual,
                SensorPeso = response.SensorPeso,
                SensorTipo = response.SensorTipo,
                SensorTermico = response.SensorTermico,
                SensorStatus = response.SensorStatus,
                MaterialSuportado = response.MaterialSuportado
            };

            return (true, "Ecoponto obtido com sucesso", ecopontoDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter ecoponto: {ex.Message}");
            return (false, "Erro ao obter ecoponto", null);
        }
    }

    private float CalcularDistancia(float lat1, float lon1, float lat2, float lon2)
    {
        const float R = 6371; // Raio da Terra em quilômetros
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (float)(R * c);
    }

    private float ToRad(float deg)
    {
        return (float)(deg * Math.PI / 180);
    }
} 