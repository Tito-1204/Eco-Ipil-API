using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;
using System.Linq; // Adicionado para .Select e .ToList etc.

namespace EcoIpil.API.Services;

// Classe auxiliar simples para buscar o nome do agente.
// Idealmente, você teria um modelo Agente.cs se fosse usar mais dados do agente.
public class AgenteInfo
{
    [ Supabase.Postgrest.Attributes.PrimaryKey("id", false) ] // false se não for identity ou se for apenas para leitura
    public long Id { get; set; }

    [ Supabase.Postgrest.Attributes.Column("nome") ]
    public string Nome { get; set; } = string.Empty;
}


public class EcopontoService
{
    private readonly Supabase.Client _supabaseClient;

    public EcopontoService(SupabaseService supabaseService)
    {
        _supabaseClient = supabaseService.GetClient();
    }

    public async Task<(bool success, string message, List<EcopontoResponseDTO>? ecopontos)> ListarEcopontos(
        float? latitude = null, // Usado para centro da busca por raio
        float? longitude = null, // Usado para centro da busca por raio
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
                .Select("id, created_at, nome, localizacao, status, capacidade, preenchido_atual, sensor_peso, sensor_tipo, sensor_termico, sensor_status, material_suportado, latitude, longitude, agente_responsavel_id"); // Selecionar todos os campos necessários

            // Aplicar filtros
            if (!string.IsNullOrEmpty(material))
            {
                query = query.Filter("material_suportado", Operator.ILike, $"%{material}%");
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Filter("status", Operator.Equals, status);
            }

            // A paginação será aplicada APÓS o filtro de distância, se houver.
            // Se não houver filtro de distância, aplicamos aqui.
            if (!(latitude.HasValue && longitude.HasValue && raio.HasValue))
            {
                int offset = (pagina - 1) * limite;
                query = query.Range(offset, offset + limite - 1);
            }

            var response = await query.Get();
            var ecopontosModel = response.Models;

            if (ecopontosModel == null || !ecopontosModel.Any())
            {
                return (true, "Nenhum ecoponto encontrado", new List<EcopontoResponseDTO>());
            }

            // Buscar nomes dos agentes responsáveis
            var agentIds = ecopontosModel
                .Where(e => e.AgenteResponsavelId.HasValue)
                .Select(e => e.AgenteResponsavelId!.Value)
                .Distinct()
                .ToList();

            var agentNamesMap = new Dictionary<long, string>();
            if (agentIds.Any())
            {
                var agentResponse = await _supabaseClient
                    .From<Agente>() // Usando a classe auxiliar AgenteInfo
                    .Select("id, nome")
                    .Filter("id", Operator.In, agentIds)
                    .Get();
                
                if (agentResponse.Models != null)
                {
                    foreach (var agent in agentResponse.Models)
                    {
                        agentNamesMap[agent.Id] = agent.Nome;
                    }
                }
            }

            var ecopontosDTO = ecopontosModel.Select(e => new EcopontoResponseDTO
            {
                Id = e.Id,
                CreatedAt = e.CreatedAt,
                Nome = e.Nome,
                Localizacao = e.Localizacao, // Mantido
                Latitude = e.Latitude,     // Novo
                Longitude = e.Longitude,   // Novo
                Status = e.Status,
                Capacidade = e.Capacidade,
                PreenchidoAtual = e.PreenchidoAtual,
                SensorPeso = e.SensorPeso,
                SensorTipo = e.SensorTipo,
                SensorTermico = e.SensorTermico,
                SensorStatus = e.SensorStatus,
                MaterialSuportado = e.MaterialSuportado,
                NomeAgenteResponsavel = e.AgenteResponsavelId.HasValue && agentNamesMap.TryGetValue(e.AgenteResponsavelId.Value, out var nomeAgente) 
                                        ? nomeAgente 
                                        : null // Novo
            }).ToList();

            // Se fornecidas coordenadas e raio, calcular distância e filtrar
            if (latitude.HasValue && longitude.HasValue && raio.HasValue)
            {
                ecopontosDTO = ecopontosDTO
                    .Select(dto =>
                    {
                        // Priorizar as novas colunas Latitude/Longitude para cálculo de distância
                        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
                        {
                            dto.Distancia = CalcularDistancia(latitude.Value, longitude.Value, dto.Latitude.Value, dto.Longitude.Value);
                        }
                        // Fallback para Localizacao string se as colunas dedicadas não estiverem preenchidas (opcional)
                        else if (dto.Localizacao != null) 
                        {
                            var coords = dto.Localizacao.Split(',');
                            if (coords.Length == 2 && 
                                float.TryParse(coords[0].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float ecoLat) && 
                                float.TryParse(coords[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float ecoLon))
                            {
                                dto.Distancia = CalcularDistancia(latitude.Value, longitude.Value, ecoLat, ecoLon);
                            }
                        }
                        return dto;
                    })
                    .Where(dto => dto.Distancia.HasValue && dto.Distancia <= raio.Value)
                    .OrderBy(dto => dto.Distancia)
                    .ToList();
                
                // Aplicar paginação APÓS o cálculo e filtro de distância
                ecopontosDTO = ecopontosDTO
                    .Skip((pagina - 1) * limite)
                    .Take(limite)
                    .ToList();
            }

            return (true, "Ecopontos obtidos com sucesso", ecopontosDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar ecopontos: {ex.Message} \n {ex.StackTrace}");
            return (false, "Erro ao listar ecopontos", null);
        }
    }

    public async Task<(bool success, string message, EcopontoResponseDTO? ecoponto)> ObterEcoponto(long id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Ecoponto>()
                .Select("id, created_at, nome, localizacao, status, capacidade, preenchido_atual, sensor_peso, sensor_tipo, sensor_termico, sensor_status, material_suportado, latitude, longitude, agente_responsavel_id")
                .Where(e => e.Id == id)
                .Single();

            if (response == null)
            {
                return (false, "Ecoponto não encontrado", null);
            }

            string? nomeAgenteResponsavel = null;
            if (response.AgenteResponsavelId.HasValue)
            {
                 var agentResponse = await _supabaseClient
                    .From<Agente>() // Usando a classe auxiliar AgenteInfo
                    .Select("nome")
                    .Filter("id", Operator.Equals, response.AgenteResponsavelId.Value)
                    .Single(); // Usar SingleOrDefault se o agente puder não existir apesar do ID
                
                if (agentResponse != null)
                {
                    nomeAgenteResponsavel = agentResponse.Nome;
                }
            }

            var ecopontoDTO = new EcopontoResponseDTO
            {
                Id = response.Id,
                CreatedAt = response.CreatedAt,
                Nome = response.Nome,
                Localizacao = response.Localizacao,
                Latitude = response.Latitude,
                Longitude = response.Longitude,
                Status = response.Status,
                Capacidade = response.Capacidade,
                PreenchidoAtual = response.PreenchidoAtual,
                SensorPeso = response.SensorPeso,
                SensorTipo = response.SensorTipo,
                SensorTermico = response.SensorTermico,
                SensorStatus = response.SensorStatus,
                MaterialSuportado = response.MaterialSuportado,
                NomeAgenteResponsavel = nomeAgenteResponsavel
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