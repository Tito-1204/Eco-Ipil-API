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

public class AtividadeService
{
    private readonly SupabaseService _supabaseService;
    private readonly ILogger<AtividadeService> _logger;

    public AtividadeService(SupabaseService supabaseService, ILogger<AtividadeService> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task<List<AtividadeResponseDTO>> GetRecentActivities(long userId, int limit = 3)
    {
        try
        {
            var client = _supabaseService.GetClient();

            // Conquistas
            var conquistasUsuarios = await client.From<ConquistasUsuarios>()
                .Where(cu => cu.UsuarioId == userId)
                .Select("data_conquista, conquista_id")
                .Order("data_conquista", Ordering.Descending)
                .Get();

            var conquistaIds = conquistasUsuarios.Models.Select(cu => cu.ConquistaId).ToList();
            List<Conquista> conquistasList;

            if (conquistaIds.Any())
            {
                var conquistasResponse = await client.From<Conquista>()
                    .Filter("id", Operator.In, conquistaIds)
                    .Select("id, nome, pontos")
                    .Get();
                conquistasList = conquistasResponse.Models;
            }
            else
            {
                conquistasList = new List<Conquista>();
                _logger.LogWarning("Nenhum conquista_id encontrado para o usuário {UserId}", userId);
            }

            var conquistaActivities = conquistasUsuarios.Models.Select(cu =>
            {
                var conquista = conquistasList.FirstOrDefault(c => c.Id == cu.ConquistaId);
                return new AtividadeResponseDTO
                {
                    Tipo = "conquista",
                    Timestamp = cu.DataConquista,
                    ConquistaNome = conquista?.Nome,
                    PontosConquista = conquista?.Pontos
                };
            }).ToList();

            // Reciclagem
            var reciclagens = await client.From<Reciclagem>()
                .Where(r => r.UsuarioId == userId)
                .Select("created_at, peso, ecoponto_id")
                .Order("created_at", Ordering.Descending)
                .Get();

            var ecopontoIds = reciclagens.Models.Select(r => r.EcopontoId).Distinct().ToList();
            List<Ecoponto> ecopontosList;

            if (ecopontoIds.Any())
            {
                var ecopontosResponse = await client.From<Ecoponto>()
                    .Filter("id", Operator.In, ecopontoIds)
                    .Select("id, nome")
                    .Get();
                ecopontosList = ecopontosResponse.Models;
            }
            else
            {
                ecopontosList = new List<Ecoponto>();
                _logger.LogWarning("Nenhum ecoponto_id encontrado para o usuário {UserId}", userId);
            }

            var reciclagemActivities = reciclagens.Models.Select(r =>
            {
                var ecoponto = ecopontosList.FirstOrDefault(e => e.Id == r.EcopontoId);
                return new AtividadeResponseDTO
                {
                    Tipo = "reciclagem",
                    Timestamp = r.CreatedAt,
                    Peso = r.Peso,
                    EcopontoNome = ecoponto?.Nome
                };
            }).ToList();

            // Recompensas
            var recompensasUsuarios = await client.From<RecompensaUsuario>()
                .Where(ru => ru.UsuarioId == userId)
                .Select("data_recompensa, recompensa_id")
                .Order("data_recompensa", Ordering.Descending)
                .Get();

            var recompensaIds = recompensasUsuarios.Models.Select(ru => ru.RecompensaId).Distinct().ToList();
            List<Recompensa> recompensasList;

            if (recompensaIds.Any())
            {
                var recompensasResponse = await client.From<Recompensa>()
                    .Filter("id", Operator.In, recompensaIds)
                    .Select("id, nome, pontos")
                    .Get();
                recompensasList = recompensasResponse.Models;
            }
            else
            {
                recompensasList = new List<Recompensa>();
                _logger.LogWarning("Nenhum recompensa_id encontrado para o usuário {UserId}", userId);
            }

            var recompensaActivities = recompensasUsuarios.Models.Select(ru =>
            {
                var recompensa = recompensasList.FirstOrDefault(r => r.Id == ru.RecompensaId);
                return new AtividadeResponseDTO
                {
                    Tipo = "recompensa",
                    Timestamp = ru.DataRecompensa,
                    RecompensaNome = recompensa?.Nome,
                    PontosRecompensa = recompensa?.Pontos
                };
            }).ToList();

            // Combinar e ordenar todas as atividades
            var allActivities = conquistaActivities
                .Concat(reciclagemActivities)
                .Concat(recompensaActivities)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();

            return allActivities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar atividades recentes para o usuário {UserId}", userId);
            throw;
        }
    }
}