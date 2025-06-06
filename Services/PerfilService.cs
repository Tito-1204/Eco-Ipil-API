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

public class PerfilService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly HistoricoService _historicoService;
    private readonly ILogger _logger;

    public PerfilService(SupabaseService supabaseService, UsuarioService usuarioService, HistoricoService historicoService, ILogger<PerfilService> logger)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _historicoService = historicoService;
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
        return (true, "Token válido", validationResult.userUid!);
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

    // 2. Obter Total de Quilos Reciclados
    public async Task<(bool success, string message, float totalReciclado)> ObterTotalReciclado(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success || userUid == null)
            {
                return (false, message, 0);
            }

            // Obter o ID do usuário com base no userUid
            if (userUid == null)
            {
                return (false, "UserUid não pode ser nulo", 0);
            }
            var (userSuccess, userMessage, userId, _) = await GetUserIdFromUid(userUid!);
            if (!userSuccess)
            {
                return (false, userMessage, 0);
            }

            // Converter userId para string para evitar erro de tipo no Supabase
            var reciclagens = await _supabaseClient
                .From<Reciclagem>()
                .Filter("usuario_id", Operator.Equals, userId.ToString()) // Converter long para string
                .Get();

            float totalReciclado = reciclagens.Models.Sum(r => r.Peso);
            return (true, "Total reciclado obtido com sucesso", totalReciclado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter total reciclado para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter total reciclado", 0);
        }
    }

    // 4. Contar Reciclagens e Conquistas
    public async Task<(bool success, string message, EstatisticasDTO dados)> ObterEstatisticasUsuario(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, new EstatisticasDTO());
            }

            // Obter o ID do usuário com base no userUid
            if (userUid == null)
            {
                return (false, "UserUid não pode ser nulo", new EstatisticasDTO());
            }
            var (userSuccess, userMessage, userId, _) = await GetUserIdFromUid(userUid);
            if (!userSuccess)
            {
                return (false, userMessage, new EstatisticasDTO());
            }

            // Contar reciclagens
            var reciclagensCount = await _supabaseClient
                .From<Reciclagem>()
                .Filter("usuario_id", Operator.Equals, userId.ToString()) // Converter long para string
                .Get();
            int totalReciclagens = reciclagensCount.Models.Count;

            // Contar conquistas
            var conquistasCount = await _supabaseClient
                .From<ConquistasUsuarios>()
                .Filter("usuario_id", Operator.Equals, userId.ToString()) // Converter long para string
                .Get();
            int totalConquistas = conquistasCount.Models.Count;

            var dados = new EstatisticasDTO
            {
                Reciclagens = totalReciclagens,
                Conquistas = totalConquistas
            };

            return (true, "Estatísticas obtidas com sucesso", dados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter estatísticas", new EstatisticasDTO());
        }
    }

    // 1. Obter Pontos Totais Acumulados
    public async Task<(bool success, string message, long pontosTotais)> ObterPontosTotais(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, 0);
            }

            var usuario = await _supabaseClient
                .From<Usuario>()
                .Filter("user_uid", Operator.Equals, userUid)
                .Single();

            if (usuario == null)
            {
                return (false, "Usuário não encontrado", 0);
            }

            return (true, "Pontos totais obtidos com sucesso", usuario.PontosTotais);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pontos totais para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter pontos totais", 0);
        }
    }

    // 3. Calcular Total de CO2 Evitado
    public async Task<(bool success, string message, float co2Evitado)> ObterCO2Evitado(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, 0);
            }

            var materiaisRecicladosResult = await _historicoService.ObterMateriaisReciclados(token);
            if (!materiaisRecicladosResult.success)
            {
                return (false, materiaisRecicladosResult.message, 0);
            }

            var materiaisReciclados = materiaisRecicladosResult.dados;
            if (materiaisReciclados == null || !materiaisReciclados.Any())
            {
                _logger.LogInformation("Nenhum dado de reciclagem encontrado para usuário {UserUid}", userUid);
                return (true, "Nenhum dado de reciclagem encontrado", 0);
            }

            // Log dos materiais reciclados recebidos
            _logger.LogInformation("Materiais reciclados para usuário {UserUid}: {Materiais}", 
                userUid, 
                string.Join(", ", materiaisReciclados.Select(m => $"{m.MaterialNome}: {m.TotalPeso}kg")));

            // Fatores de CO2 por material (alinhados com a tabela materiais)
            var co2Factors = new Dictionary<string, float>
            {
                { "Plástico", 2.5f },
                { "Papel", 1.2f },
                { "Vidro", 0.9f },
                { "Metal", 3.0f }
            };

            float totalCO2Evitado = 0;
            foreach (var material in materiaisReciclados)
            {
                if (co2Factors.TryGetValue(material.MaterialNome, out var factor))
                {
                    float co2 = material.TotalPeso * factor;
                    totalCO2Evitado += co2;
                    _logger.LogInformation("Material: {Nome}, Peso: {Peso}, Fator: {Fator}, CO2: {Co2}", 
                        material.MaterialNome, material.TotalPeso, factor, co2);
                }
                else
                {
                    _logger.LogWarning("Fator de CO2 não encontrado para material: {Nome}", material.MaterialNome);
                }
            }

            _logger.LogInformation("CO2 evitado calculado para usuário {UserUid}: {TotalCO2} kg", userUid, totalCO2Evitado);
            return (true, "CO2 evitado calculado com sucesso", totalCO2Evitado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular CO2 evitado para o usuário {UserUid}", userUid);
            return (false, "Erro ao calcular CO2 evitado", 0);
        }
    }

    // 5. Determinar o Nível do Usuário
    public async Task<(bool success, string message, int nivel)> ObterNivelUsuario(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, 0);
            }

            var pontosResult = await ObterPontosTotais(token);
            if (!pontosResult.success)
            {
                return (false, pontosResult.message, 0);
            }

            long pontosTotais = pontosResult.pontosTotais;

            // Calcular o nível com base na fórmula: pontos = 5000 * n^2
            int nivel = 0;
            for (int n = 1; n <= 100; n++)
            {
                long pontosNecessarios = 5000 * (long)n * n;
                if (pontosTotais < pontosNecessarios)
                {
                    break;
                }
                nivel = n;
            }

            return (true, "Nível do usuário obtido com sucesso", nivel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter nível para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter nível", 0);
        }
    }

    // 6. Endpoint Combinado
    public async Task<(bool success, string message, PerfilUsuarioDTO? dados)> ObterPerfilUsuario(string token)
    {
        string? userUid = null;
        try
        {
            var (success, message, validatedUserUid) = await ValidateTokenForUid(token);
            userUid = validatedUserUid;
            if (!success)
            {
                return (false, message, null);
            }

            var pontosTask = ObterPontosTotais(token);
            var recicladoTask = ObterTotalReciclado(token);
            var co2Task = ObterCO2Evitado(token);
            var estatisticasTask = ObterEstatisticasUsuario(token);
            var nivelTask = ObterNivelUsuario(token);

            await Task.WhenAll(pontosTask, recicladoTask, co2Task, estatisticasTask, nivelTask);

            if (!pontosTask.Result.success || !recicladoTask.Result.success ||
                !co2Task.Result.success || !estatisticasTask.Result.success ||
                !nivelTask.Result.success)
            {
                return (false, "Erro ao obter dados do perfil", null);
            }

            var dados = new PerfilUsuarioDTO
            {
                PontosTotais = pontosTask.Result.pontosTotais,
                TotalReciclado = recicladoTask.Result.totalReciclado,
                CO2Evitado = co2Task.Result.co2Evitado,
                Estatisticas = estatisticasTask.Result.dados,
                Nivel = nivelTask.Result.nivel
            };

            return (true, "Perfil obtido com sucesso", dados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter perfil para o usuário {UserUid}", userUid);
            return (false, "Erro ao obter perfil", null);
        }
    }
}