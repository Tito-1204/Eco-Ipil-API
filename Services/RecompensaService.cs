using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Responses;
using System.Linq;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class RecompensaService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly CarteiraService _carteiraService;

    public RecompensaService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        CarteiraService carteiraService)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _carteiraService = carteiraService;
    }

    /// <summary>
    /// Lista todas as recompensas disponíveis com filtros opcionais (requer autenticação)
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaResponseDTO>? recompensas)> ListarRecompensas(
        string token,
        string? tipo = null,
        long? precoMin = null,
        long? precoMax = null)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            // Iniciar a consulta
            var query = _supabaseClient.From<Recompensa>().Select("*");

            // Aplicar filtros
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Filter("tipo", Operator.Equals, tipo);
            }

            if (precoMin.HasValue)
            {
                query = query.Filter("pontos", Operator.GreaterThanOrEqual, (int)precoMin.Value);
            }

            if (precoMax.HasValue)
            {
                query = query.Filter("pontos", Operator.LessThanOrEqual, (int)precoMax.Value);
            }

            // Executar a consulta
            var recompensas = await query.Get();

            if (recompensas == null || !recompensas.Models.Any())
            {
                return (true, "Nenhuma recompensa encontrada", new List<RecompensaResponseDTO>());
            }

            // Mapear para o DTO de resposta
            var recompensasDTO = recompensas.Models.Select(r => new RecompensaResponseDTO
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                Nome = r.Nome,
                Tipo = r.Tipo,
                Descricao = r.Descricao,
                Pontos = r.Pontos,
                QtRestante = r.QtRestante
            }).ToList();

            return (true, "Recompensas listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas: {ex.Message}");
            return (false, $"Erro ao listar recompensas: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Lista todas as recompensas disponíveis sem exigir autenticação (usado para notificações gerais)
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaResponseDTO>? recompensas)> ListarRecompensasSemAutenticacao(
        string? tipo = null,
        long? precoMin = null,
        long? precoMax = null)
    {
        try
        {
            // Iniciar a consulta
            var query = _supabaseClient.From<Recompensa>().Select("*");

            // Aplicar filtros
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Filter("tipo", Operator.Equals, tipo);
            }

            if (precoMin.HasValue)
            {
                query = query.Filter("pontos", Operator.GreaterThanOrEqual, (int)precoMin.Value);
            }

            if (precoMax.HasValue)
            {
                query = query.Filter("pontos", Operator.LessThanOrEqual, (int)precoMax.Value);
            }

            // Executar a consulta
            var recompensas = await query.Get();

            if (recompensas == null || !recompensas.Models.Any())
            {
                return (true, "Nenhuma recompensa encontrada", new List<RecompensaResponseDTO>());
            }

            // Mapear para o DTO de resposta
            var recompensasDTO = recompensas.Models.Select(r => new RecompensaResponseDTO
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                Nome = r.Nome,
                Tipo = r.Tipo,
                Descricao = r.Descricao,
                Pontos = r.Pontos,
                QtRestante = r.QtRestante
            }).ToList();

            return (true, "Recompensas listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas sem autenticação: {ex.Message}");
            return (false, $"Erro ao listar recompensas sem autenticação: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Obtém os detalhes de uma recompensa específica
    /// </summary>
    public async Task<(bool success, string message, RecompensaResponseDTO? recompensa)> ObterRecompensa(
        string token,
        long recompensaId)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            // Buscar a recompensa
            var recompensa = await _supabaseClient
                .From<Recompensa>()
                .Where(r => r.Id == recompensaId)
                .Single();

            if (recompensa == null)
            {
                return (false, "Recompensa não encontrada", null);
            }

            // Mapear para o DTO de resposta
            var recompensaDTO = new RecompensaResponseDTO
            {
                Id = recompensa.Id,
                CreatedAt = recompensa.CreatedAt,
                Nome = recompensa.Nome,
                Tipo = recompensa.Tipo,
                Descricao = recompensa.Descricao,
                Pontos = recompensa.Pontos,
                QtRestante = recompensa.QtRestante
            };

            return (true, "Recompensa encontrada com sucesso", recompensaDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter recompensa: {ex.Message}");
            return (false, $"Erro ao obter recompensa: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Resgata uma recompensa para o usuário
    /// </summary>
    public async Task<(bool success, string message, object? data)> ResgatarRecompensa(
        string token,
        long recompensaId)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            long userId = validationResult.userId;

            // Verificar se a recompensa existe e tem estoque
            var recompensa = await _supabaseClient
                .From<Recompensa>()
                .Where(r => r.Id == recompensaId)
                .Single();

            if (recompensa == null)
            {
                return (false, "Recompensa não encontrada", null);
            }

            if (recompensa.QtRestante <= 0)
            {
                return (false, "Esta recompensa não está mais disponível (estoque esgotado)", null);
            }

            // Obter a carteira do usuário para verificar os pontos
            var carteiraResult = await _carteiraService.ObterCarteira(token);
            if (!carteiraResult.success || carteiraResult.carteira == null)
            {
                return (false, "Não foi possível verificar sua carteira digital", null);
            }

            var carteira = carteiraResult.carteira;
            if (carteira.Pontos < recompensa.Pontos)
            {
                return (false, $"Pontos insuficientes. Você possui {carteira.Pontos} pontos, mas precisa de {recompensa.Pontos} pontos.", null);
            }

            // Iniciar uma transação (idealmente, mas não temos suporte direto do Supabase para isso)
            // Então vamos fazer uma série de operações em sequência

            // 1. Debitar os pontos da carteira do usuário
            var carteiraAtual = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteiraAtual == null)
            {
                return (false, "Carteira digital não encontrada", null);
            }

            // Atualizar o saldo de pontos
            carteiraAtual.Pontos -= recompensa.Pontos;
            await _supabaseClient.From<CarteiraDigital>().Update(carteiraAtual);

            // 2. Diminuir o estoque da recompensa
            recompensa.QtRestante -= 1;
            await _supabaseClient.From<Recompensa>().Update(recompensa);

            // 3. Registrar o resgate na tabela associativa
            try
            {
                Console.WriteLine($"Tentando criar registro na tabela recompensas_usuarios: RecompensaId={recompensaId}, UsuarioId={userId}, DataRecompensa={DateTime.UtcNow}");
                
                var recompensaUsuario = new RecompensaUsuario
                {
                    RecompensaId = recompensaId,
                    UsuarioId = userId,
                    DataRecompensa = DateTime.UtcNow
                };

                // Verificar se os valores são válidos antes de inserir
                if (recompensaId <= 0 || userId <= 0)
                {
                    return (false, $"Valores inválidos: RecompensaId={recompensaId}, UsuarioId={userId}", null);
                }

                // Inserir o registro usando a tabela correta e garantindo os campos corretamente
                var insertResponse = await _supabaseClient
                    .From<RecompensaUsuario>()
                    .Insert(recompensaUsuario);

                // Verificar se a inserção foi bem-sucedida
                if (insertResponse == null || !insertResponse.Models.Any())
                {
                    return (false, "Falha ao registrar a recompensa na conta do usuário", null);
                }

                Console.WriteLine("Registro criado com sucesso na tabela recompensas_usuarios");
            }
            catch (Exception ex)
            {
                // Reverter operações anteriores
                carteiraAtual.Pontos += recompensa.Pontos;
                await _supabaseClient.From<CarteiraDigital>().Update(carteiraAtual);
                
                recompensa.QtRestante += 1;
                await _supabaseClient.From<Recompensa>().Update(recompensa);
                
                Console.WriteLine($"Erro ao registrar na tabela recompensas_usuarios: {ex.Message}");
                return (false, $"Erro ao registrar o resgate da recompensa: {ex.Message}", null);
            }

            // 4. Preparar a resposta com os detalhes do resgate
            var recompensaResgatada = new RecompensaUsuarioResponseDTO
            {
                Id = recompensa.Id,
                Nome = recompensa.Nome,
                Tipo = recompensa.Tipo,
                Descricao = recompensa.Descricao,
                Pontos = recompensa.Pontos,
                DataResgate = DateTime.UtcNow,
                Status = "Resgatada"
            };

            // Retornar sucesso com os dados da recompensa resgatada
            return (true, "Recompensa resgatada com sucesso", recompensaResgatada);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao resgatar recompensa: {ex.Message}");
            return (false, $"Erro ao resgatar recompensa: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Lista as recompensas resgatadas pelo usuário
    /// </summary>
    public async Task<(bool success, string message, List<RecompensaUsuarioResponseDTO>? recompensas)> ListarRecompensasUsuario(
        string token,
        string? status = null)
    {
        try
        {
            // Validar o token
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }

            long userId = validationResult.userId;

            // Buscar os registros de recompensas do usuário
            var query = _supabaseClient
                .From<RecompensaUsuario>()
                .Where(ru => ru.UsuarioId == userId);

            var recompensasUsuario = await query.Get();

            if (recompensasUsuario == null || !recompensasUsuario.Models.Any())
            {
                return (true, "Nenhuma recompensa resgatada encontrada", new List<RecompensaUsuarioResponseDTO>());
            }

            // Buscar os detalhes de cada recompensa
            var recompensasDTO = new List<RecompensaUsuarioResponseDTO>();
            foreach (var ru in recompensasUsuario.Models)
            {
                var recompensa = await _supabaseClient
                    .From<Recompensa>()
                    .Where(r => r.Id == ru.RecompensaId)
                    .Single();

                if (recompensa != null)
                {
                    var dto = new RecompensaUsuarioResponseDTO
                    {
                        Id = recompensa.Id,
                        Nome = recompensa.Nome,
                        Tipo = recompensa.Tipo,
                        Descricao = recompensa.Descricao,
                        Pontos = recompensa.Pontos,
                        DataResgate = ru.DataRecompensa,
                        Status = "Resgatada" // Poderia ter lógica para diferenciar status
                    };

                    // Aplicar filtro por status, se fornecido
                    if (string.IsNullOrEmpty(status) || dto.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    {
                        recompensasDTO.Add(dto);
                    }
                }
            }

            return (true, "Recompensas do usuário listadas com sucesso", recompensasDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar recompensas do usuário: {ex.Message}");
            return (false, $"Erro ao listar recompensas do usuário: {ex.Message}", null);
        }
    }
}