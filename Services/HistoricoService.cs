using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class HistoricoService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly ILogger _logger;

    public HistoricoService(SupabaseService supabaseService, UsuarioService usuarioService, ILogger<HistoricoService> logger)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _logger = logger;
    }

    // Método auxiliar para validar o token e obter o userUid
    private async Task<(bool success, string message, string? userUid)> ValidateTokenForUid(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, "Token não pode ser nulo ou vazio", null);
        }

        var validationResult = await _usuarioService.ValidateTokenForUid(token);
        if (!validationResult.success)
        {
            return (false, validationResult.message, null);
        }
        return (true, "Token válido", validationResult.userUid);
    }

    // Método auxiliar para obter o id do usuário com base no userUid
    private async Task<(bool success, string message, long userId, string userUid)> GetUserIdFromUid(string userUid)
    {
        try
        {
            var usuario = await _supabaseClient
                .From<Usuario>()
                .Filter("user_uid", Operator.Equals, userUid)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", 0, userUid);
            }

            return (true, "Usuário encontrado", usuario.Id, userUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ID do usuário com userUid {UserUid}", userUid);
            return (false, "Erro ao buscar ID do usuário", 0, userUid);
        }
    }

    public async Task<(bool success, string message, List<ReciclagemResponseDTO>? reciclagens)> ObterHistoricoReciclagem(
        string token,
        long? materialId = null,
        long? ecopontoId = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        int pagina = 1,
        int limite = 10)
    {
        try
        {
            // Validar token e obter ID do usuário
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;

            if (!success)
            {
                return (false, message, null);
            }

            Console.WriteLine($"Obtendo reciclagens para usuário ID: {userId}");

            try
            {
                // Obter todas as reciclagens do usuário
                var response = await _supabaseClient
                    .From<Reciclagem>()
                    .Where(r => r.UsuarioId == userId)
                    .Order("created_at", Ordering.Descending)
                    .Get();

                var todasReciclagens = response.Models?.ToList() ?? new List<Reciclagem>();
                Console.WriteLine($"Total de reciclagens encontradas: {todasReciclagens.Count}");

                // Filtrar os resultados em memória
                var reciclagensFiltradas = todasReciclagens;

                // Aplicar filtros
                if (materialId.HasValue)
                {
                    Console.WriteLine($"Filtrando por material: {materialId.Value}");
                    reciclagensFiltradas = reciclagensFiltradas
                        .Where(r => r.MaterialId == materialId.Value)
                        .ToList();
                }

                if (ecopontoId.HasValue)
                {
                    Console.WriteLine($"Filtrando por ecoponto: {ecopontoId.Value}");
                    reciclagensFiltradas = reciclagensFiltradas
                        .Where(r => r.EcopontoId == ecopontoId.Value)
                        .ToList();
                }

                if (dataInicio.HasValue)
                {
                    Console.WriteLine($"Filtrando por data inicial: {dataInicio.Value:yyyy-MM-dd HH:mm:ss}");
                    reciclagensFiltradas = reciclagensFiltradas
                        .Where(r => r.CreatedAt >= dataInicio.Value)
                        .ToList();
                }

                if (dataFim.HasValue)
                {
                    Console.WriteLine($"Filtrando por data final: {dataFim.Value:yyyy-MM-dd HH:mm:ss}");
                    reciclagensFiltradas = reciclagensFiltradas
                        .Where(r => r.CreatedAt <= dataFim.Value)
                        .ToList();
                }

                Console.WriteLine($"Reciclagens após aplicação dos filtros: {reciclagensFiltradas.Count}");

                // Aplicar paginação em memória
                var reciclagensPaginadas = reciclagensFiltradas
                    .Skip((pagina - 1) * limite)
                    .Take(limite)
                    .ToList();

                Console.WriteLine($"Reciclagens após paginação: {reciclagensPaginadas.Count}");

                if (!reciclagensPaginadas.Any())
                {
                    return (true, "Nenhum registro de reciclagem encontrado", new List<ReciclagemResponseDTO>());
                }

                // Obter IDs únicos para consultas relacionadas
                var materialIds = reciclagensPaginadas.Select(r => r.MaterialId).Distinct().ToList();
                var ecopontoIds = reciclagensPaginadas.Select(r => r.EcopontoId).Distinct().ToList();
                var agenteIds = reciclagensPaginadas.Where(r => r.AgenteId.HasValue)
                    .Select(r => r.AgenteId!.Value)
                    .Distinct()
                    .ToList();

                // Obter materiais relacionados
                var materiaisResponse = await _supabaseClient
                    .From<Material>()
                    .Filter("id", Operator.In, materialIds)
                    .Get();
                var materiais = materiaisResponse.Models?.ToDictionary(m => m.Id, m => m) ?? new Dictionary<long, Material>();

                // Obter ecopontos relacionados
                var ecopontosResponse = await _supabaseClient
                    .From<Ecoponto>()
                    .Filter("id", Operator.In, ecopontoIds)
                    .Get();
                var ecopontos = ecopontosResponse.Models?.ToDictionary(e => e.Id, e => e) ?? new Dictionary<long, Ecoponto>();

                // Obter agentes relacionados (se houver)
                var agentes = new Dictionary<long, Agente>();
                if (agenteIds.Any())
                {
                    var agentesResponse = await _supabaseClient
                        .From<Agente>()
                        .Filter("id", Operator.In, agenteIds)
                        .Get();
                    agentes = agentesResponse.Models?.ToDictionary(a => a.Id, a => a) ?? new Dictionary<long, Agente>();
                }

                // Mapear para DTOs
                var reciclagensDTO = reciclagensPaginadas.Select(r =>
                {
                    var dto = new ReciclagemResponseDTO
                    {
                        Id = r.Id,
                        CreatedAt = r.CreatedAt,
                        Peso = r.Peso,
                        UsuarioId = r.UsuarioId,
                        MaterialId = r.MaterialId,
                        EcopontoId = r.EcopontoId,
                        AgenteId = r.AgenteId
                    };

                    // Adicionar informações do material
                    if (materiais.TryGetValue(r.MaterialId, out var material))
                    {
                        dto.MaterialNome = material.Nome;
                        dto.MaterialClasse = material.Classe;
                        dto.PontosGanhos = (long)(r.Peso * material.Valor);
                    }

                    // Adicionar informações do ecoponto
                    if (ecopontos.TryGetValue(r.EcopontoId, out var ecoponto))
                    {
                        dto.EcopontoNome = ecoponto.Nome;
                        dto.EcopontoLocalizacao = ecoponto.Localizacao;
                    }

                    // Adicionar informações do agente (se houver)
                    if (r.AgenteId.HasValue && agentes.TryGetValue(r.AgenteId.Value, out var agente))
                    {
                        dto.AgenteNome = agente.Nome;
                    }

                    return dto;
                }).ToList();

                return (true, "Histórico de reciclagem obtido com sucesso", reciclagensDTO);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a consulta: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter histórico de reciclagem: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return (false, $"Erro ao obter histórico de reciclagem: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, HistoricoEstatisticasDTO? estatisticas)> ObterEstatisticas(string token)
    {
        try
        {
            // Validar token e obter ID do usuário
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;

            if (!success)
            {
                return (false, message, null);
            }

            // Obter todas as reciclagens do usuário
            var reciclagensResponse = await _supabaseClient
                .From<Reciclagem>()
                .Where(r => r.UsuarioId == userId)
                .Get();
            var reciclagens = reciclagensResponse.Models;

            if (reciclagens == null || !reciclagens.Any())
            {
                return (true, "Nenhum registro de reciclagem encontrado", new HistoricoEstatisticasDTO());
            }

            // Obter materiais para calcular pontos
            var materialIds = reciclagens.Select(r => r.MaterialId).Distinct().ToList();
            var materiaisResponse = await _supabaseClient
                .From<Material>()
                .Filter("id", Operator.In, materialIds)
                .Get();
            var materiais = materiaisResponse.Models?.ToDictionary(m => m.Id, m => m) ?? new Dictionary<long, Material>();

            // Calcular estatísticas
            float totalReciclado = reciclagens.Sum(r => r.Peso);
            long pontosAcumulados = reciclagens.Sum(r =>
                materiais.TryGetValue(r.MaterialId, out var material) ? (long)(r.Peso * material.Valor) : 0);
            int visitasEcoponto = reciclagens.Select(r => r.EcopontoId).Distinct().Count();
            int arvoresSalvas = (int)Math.Floor(totalReciclado / 50);

            var estatisticas = new HistoricoEstatisticasDTO
            {
                TotalReciclado = totalReciclado,
                PontosAcumulados = pontosAcumulados,
                VisitasEcoponto = visitasEcoponto,
                ArvoresSalvas = arvoresSalvas
            };

            return (true, "Estatísticas obtidas com sucesso", estatisticas);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter estatísticas: {ex.Message}");
            return (false, "Erro ao obter estatísticas", null);
        }
    }

    public async Task<(bool success, string message, HistoricoGraficoDTO? grafico)> ObterDadosGrafico(
        string token,
        string periodo,
        int ano,
        int? mes)
    {
        // Se 'mes' tiver valor, pode ser usado; caso contrário, será definido abaixo
        try
        {
            // Validar token e obter ID do usuário
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;

            if (!success)
            {
                return (false, message, null);
            }

            // Definir período de análise
            DateTime dataInicio;
            DateTime dataFim = DateTime.UtcNow;
            List<string> labels = new List<string>();

            int anoAtual = ano; // 'ano' já é não-nulo
            int mesAtual = mes ?? DateTime.UtcNow.Month;

            switch (periodo.ToLower())
            {
                case "semanal":
                    dataInicio = dataFim.AddDays(-6);
                    for (int i = 0; i < 7; i++)
                    {
                        labels.Add(dataInicio.AddDays(i).ToString("dd/MM"));
                    }
                    break;
                case "anual":
                    dataInicio = new DateTime(anoAtual, 1, 1);
                    dataFim = new DateTime(anoAtual, 12, 31);
                    for (int i = 1; i <= 12; i++)
                    {
                        labels.Add(new DateTime(anoAtual, i, 1).ToString("MMM"));
                    }
                    break;
                case "mensal":
                default:
                    dataInicio = new DateTime(anoAtual, mesAtual, 1);
                    dataFim = dataInicio.AddMonths(1).AddDays(-1);
                    int diasNoMes = DateTime.DaysInMonth(anoAtual, mesAtual);
                    for (int i = 1; i <= diasNoMes; i++)
                    {
                        labels.Add(i.ToString());
                    }
                    break;
            }

            var reciclagensResponse = await _supabaseClient
                .From<Reciclagem>()
                .Where(r => r.UsuarioId == userId)
                .Filter("created_at", Operator.GreaterThanOrEqual, dataInicio.ToString("o"))
                .Filter("created_at", Operator.LessThanOrEqual, dataFim.ToString("o"))
                .Get();
            var reciclagens = reciclagensResponse.Models ?? new List<Reciclagem>();

            List<float> dados = new List<float>();

            switch (periodo.ToLower())
            {
                case "semanal":
                    for (int i = 0; i < 7; i++)
                    {
                        var dia = dataInicio.AddDays(i).Date;
                        float pesoDia = reciclagens
                            .Where(r => r.CreatedAt.Date == dia)
                            .Sum(r => r.Peso);
                        dados.Add(pesoDia);
                    }
                    break;
                case "anual":
                    for (int i = 1; i <= 12; i++)
                    {
                        float pesoMes = reciclagens
                            .Where(r => r.CreatedAt.Month == i && r.CreatedAt.Year == anoAtual)
                            .Sum(r => r.Peso);
                        dados.Add(pesoMes);
                    }
                    break;
                case "mensal":
                default:
                    int diasNoMes = DateTime.DaysInMonth(anoAtual, mesAtual);
                    for (int i = 1; i <= diasNoMes; i++)
                    {
                        var dia = new DateTime(anoAtual, mesAtual, i);
                        float pesoDia = reciclagens
                            .Where(r => r.CreatedAt.Date == dia.Date)
                            .Sum(r => r.Peso);
                        dados.Add(pesoDia);
                    }
                    break;
            }

            var grafico = new HistoricoGraficoDTO
            {
                Labels = labels,
                Dados = dados
            };

            return (true, "Dados do gráfico obtidos com sucesso", grafico);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter dados do gráfico: {ex.Message}");
            return (false, "Erro ao obter dados do gráfico", null);
        }
    }

    // 1. Obter Materiais Reciclados (para gráfico)
    public async Task<(bool success, string message, List<MaterialRecicladoDTO> dados)> ObterMateriaisReciclados(string token)
    {
        string userUid = string.Empty;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            if (validatedUserUid == null)
            {
                return (false, "User ID não encontrado", new List<MaterialRecicladoDTO>());
            }
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, new List<MaterialRecicladoDTO>());
            }

            // Obter o ID do usuário com base no userUid
            var (userSuccess, userMessage, userId, _) = await GetUserIdFromUid(userUid);
            if (!userSuccess)
            {
                return (false, userMessage, new List<MaterialRecicladoDTO>());
            }

            // Buscar todas as reciclagens do usuário
            var reciclagensResponse = await _supabaseClient
                .From<Reciclagem>()
                .Filter("usuario_id", Operator.Equals, userId.ToString()) // Converter long para string
                .Get();

            var reciclagens = reciclagensResponse.Models;
            if (reciclagens == null || !reciclagens.Any())
            {
                return (true, "Nenhum dado de reciclagem encontrado", new List<MaterialRecicladoDTO>());
            }

            // Obter os materiais correspondentes
            var materialIds = reciclagens.Select(r => r.MaterialId).Distinct().ToList();
            var materiaisResponse = await _supabaseClient
                .From<Material>()
                .Filter("id", Operator.In, materialIds.Select(id => id.ToString()).ToList()) // Converter cada ID para string
                .Get();

            var materiais = materiaisResponse.Models?.ToDictionary(m => m.Id, m => m.Nome) ?? new Dictionary<long, string>();

            // Agrupar por material e calcular o total de peso
            var somaPorMaterial = reciclagens
                .GroupBy(r => r.MaterialId)
                .Select(g => new MaterialRecicladoDTO
                {
                    MaterialNome = materiais.TryGetValue(g.Key, out var nome) ? nome : "Desconhecido",
                    TotalPeso = g.Sum(r => r.Peso)
                })
                .ToList();

            return (true, "Dados obtidos com sucesso", somaPorMaterial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter materiais reciclados para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter dados", new List<MaterialRecicladoDTO>());
        }
    }

    // 2. Obter Reciclagem dos Últimos 6 Meses (para gráfico)
    public async Task<(bool success, string message, List<ReciclagemMensalDTO> dados)> ObterReciclagemUltimos6Meses(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            if (!success || validatedUserUid == null)
            {
                return (false, message, new List<ReciclagemMensalDTO>());
            }

            userUid = validatedUserUid;

            // Obter o ID do usuário com base no userUid
            var (userSuccess, userMessage, userId, _) = await GetUserIdFromUid(userUid);
            if (!userSuccess)
            {
                return (false, userMessage, new List<ReciclagemMensalDTO>());
            }

            // Calcular a data de início (6 meses atrás)
            var dataInicio = DateTime.UtcNow.AddMonths(-6);

            // Buscar reciclagens dos últimos 6 meses
            var reciclagensResponse = await _supabaseClient
                .From<Reciclagem>()
                .Filter("usuario_id", Operator.Equals, userId.ToString()) // Converter long para string
                .Filter("created_at", Operator.GreaterThanOrEqual, dataInicio.ToString("o"))
                .Get();

            var reciclagens = reciclagensResponse.Models;
            if (reciclagens == null || !reciclagens.Any())
            {
                return (true, "Nenhum dado de reciclagem encontrado nos últimos 6 meses", new List<ReciclagemMensalDTO>());
            }

            // Agrupar por mês e calcular o total de peso
            var somaPorMes = reciclagens
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new
                {
                    Ano = g.Key.Year,
                    Mes = g.Key.Month,
                    TotalPeso = g.Sum(r => r.Peso)
                })
                .ToList();

            // Gerar lista dos últimos 6 meses
            var meses = Enumerable.Range(0, 6)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .Select(d => new { d.Year, d.Month })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            // Mapear os dados, incluindo meses sem reciclagem (com total 0)
            var resultado = meses.Select(m => new ReciclagemMensalDTO
            {
                Mes = new DateTime(m.Year, m.Month, 1).ToString("MMM/yyyy"),
                TotalPeso = somaPorMes.FirstOrDefault(s => s.Ano == m.Year && s.Mes == m.Month)?.TotalPeso ?? 0
            }).ToList();

            return (true, "Dados obtidos com sucesso", resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter reciclagem dos últimos 6 meses para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter dados", new List<ReciclagemMensalDTO>());
        }
    }
}
