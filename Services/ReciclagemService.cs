using System;
using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using System.Linq;
using static Supabase.Postgrest.Constants;
using Supabase.Postgrest;
using System.Collections.Generic;

namespace EcoIpil.API.Services;

public class ReciclagemService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly MaterialService _materialService;
    private readonly EcopontoService _ecopontoService;
    private readonly CampanhaService _campanhaService;

    private readonly Dictionary<long, Dictionary<string, long>> ajustesPontos = new()
    {
        { 1, new Dictionary<string, long> { { "ruim", -200 }, { "moderada", 0 }, { "boa", 200 }, { "excelente", 600 } } },
        { 2, new Dictionary<string, long> { { "ruim", -80 }, { "moderada", 0 }, { "boa", 120 }, { "excelente", 260 } } },
        { 3, new Dictionary<string, long> { { "ruim", -100 }, { "moderada", 0 }, { "boa", 100 }, { "excelente", 200 } } },
        { 4, new Dictionary<string, long> { { "ruim", -40 }, { "moderada", 0 }, { "boa", 40 }, { "excelente", 100 } } },
        { 5, new Dictionary<string, long> { { "ruim", -100 }, { "moderada", 0 }, { "boa", 100 }, { "excelente", 300 } } },
        { 6, new Dictionary<string, long> { { "ruim", -60 }, { "moderada", 0 }, { "boa", 80 }, { "excelente", 140 } } },
        { 7, new Dictionary<string, long> { { "ruim", -1000 }, { "moderada", 0 }, { "boa", 1000 }, { "excelente", 2000 } } }
    };

    public ReciclagemService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        MaterialService materialService,
        EcopontoService ecopontoService,
        CampanhaService campanhaService)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _materialService = materialService;
        _ecopontoService = ecopontoService;
        _campanhaService = campanhaService;
    }

    public async Task<(bool success, string message, object? data)> EscanearQR(string token, string codigoQR)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;

            if (!success)
            {
                return (false, message, null);
            }

            var partes = codigoQR.Split(':');
            if (partes.Length < 4)
            {
                return (false, "Código QR inválido ou mal formatado. Formato esperado: agente_id:ecoponto_id:material_id:qualidade[:peso]", null);
            }

            if (!long.TryParse(partes[0], out long agenteId))
            {
                return (false, "ID do agente inválido no código QR", null);
            }

            if (!long.TryParse(partes[1], out long ecopontoId))
            {
                return (false, "ID do ecoponto inválido no código QR", null);
            }

            if (!long.TryParse(partes[2], out long materialId))
            {
                return (false, "ID do material inválido no código QR", null);
            }

            string qualidade = partes[3].ToLower();
            if (!new[] { "ruim", "moderada", "boa", "excelente" }.Contains(qualidade))
            {
                return (false, "Qualidade inválida. Deve ser 'ruim', 'moderada', 'boa' ou 'excelente'.", null);
            }

            float peso = 0;
            if (partes.Length > 4 && !float.TryParse(partes[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out peso))
            {
                peso = 0;
            }

            if (peso <= 0)
            {
                return (false, "Peso inválido. É necessário informar um peso maior que zero para registrar a reciclagem.", null);
            }

            string? agenteNome = null;
            try
            {
                var agente = await _supabaseClient
                    .From<Agente>()
                    .Where(a => a.Id == agenteId)
                    .Single();

                if (agente == null)
                {
                    return (false, "Agente não encontrado", null);
                }
                agenteNome = agente.Nome;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar agente: {ex.Message}");
                return (false, "Erro ao verificar agente", null);
            }

            var (ecopontoSuccess, ecopontoMessage, ecoponto) = await _ecopontoService.ObterEcoponto(ecopontoId);
            if (!ecopontoSuccess || ecoponto == null)
            {
                return (false, "Ecoponto não encontrado", null);
            }

            var (materialSuccess, materialMessage, material) = await _materialService.ObterMaterial(materialId);
            if (!materialSuccess || material == null)
            {
                return (false, "Material não encontrado", null);
            }

            if (!ajustesPontos.ContainsKey(materialId) || !ajustesPontos[materialId].ContainsKey(qualidade))
            {
                return (false, "Material ou qualidade não suportados.", null);
            }

            long ajustePontos = ajustesPontos[materialId][qualidade];
            long pontosBase = (long)(peso * material.Valor);
            long pontosGanhos = pontosBase + ajustePontos;

            try
            {
                var novaReciclagem = new Reciclagem
                {
                    CreatedAt = DateTime.UtcNow,
                    Peso = peso,
                    UsuarioId = userId,
                    MaterialId = materialId,
                    EcopontoId = ecopontoId,
                    AgenteId = agenteId
                };

                var response = await _supabaseClient.From<Reciclagem>()
                    .Insert(novaReciclagem);
                var reciclagemInserida = response.Models.FirstOrDefault();

                if (reciclagemInserida == null)
                {
                    return (false, "Erro ao inserir reciclagem: não foi possível recuperar o registro inserido", null);
                }

                await _usuarioService.AtualizarPontos(userId, pontosGanhos);

                var reciclagemDTO = new ReciclagemResponseDTO
                {
                    Id = reciclagemInserida.Id,
                    CreatedAt = reciclagemInserida.CreatedAt,
                    Peso = peso,
                    UsuarioId = userId,
                    MaterialId = materialId,
                    EcopontoId = ecopontoId,
                    AgenteId = agenteId,
                    MaterialNome = material.Nome,
                    MaterialClasse = material.Classe,
                    PontosGanhos = pontosGanhos,
                    EcopontoNome = ecoponto.Nome,
                    EcopontoLocalizacao = ecoponto.Localizacao,
                    AgenteNome = agenteNome
                };

                var participacaoAtiva = await _supabaseClient.From<UsuarioCampanha>()
                    .Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(userId))
                    .Filter("status", Operator.NotEqual, "Completa")
                    .Single();

                if (participacaoAtiva != null)
                {
                    Console.WriteLine($"Participação ativa encontrada: CampanhaId={participacaoAtiva.CampanhaId}, UsuarioId={participacaoAtiva.UsuarioId}, Status={participacaoAtiva.Status}");

                    try
                    {
                        await _supabaseClient.Postgrest.Rpc("inserir_campanha_reciclagem", 
                            new { p_campanha_id = participacaoAtiva.CampanhaId, p_reciclagem_id = reciclagemInserida.Id });

                        Console.WriteLine("Associação CampanhaReciclagem inserida com sucesso via função RPC");

                        if (_campanhaService != null)
                        {
                            var (verifSuccess, verifMessage) = await _campanhaService.VerificarCumprimentoCampanha(userId, participacaoAtiva.CampanhaId);
                            Console.WriteLine($"Resultado da verificação da campanha: Success={verifSuccess}, Message={verifMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao associar reciclagem à campanha via RPC: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Nenhuma participação ativa encontrada para o usuário");
                }

                return (true, "Código QR processado e reciclagem registrada com sucesso", new
                {
                    registroReciclagem = reciclagemDTO,
                    detalhes = new
                    {
                        agente = new { id = agenteId, nome = agenteNome },
                        ecoponto = new { id = ecopontoId, nome = ecoponto.Nome, localizacao = ecoponto.Localizacao },
                        material = new { 
                            id = materialId, 
                            nome = material.Nome, 
                            classe = material.Classe,
                            valor = material.Valor
                        },
                        peso,
                        qualidade,
                        pontosGanhos,
                        usuario = new { id = userId }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar reciclagem durante escaneamento: {ex.Message}");
                return (false, $"Erro ao registrar reciclagem: {ex.Message}", null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar código QR: {ex.Message}");
            return (false, $"Erro ao processar código QR: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, ReciclagemResponseDTO? reciclagem)> RegistrarReciclagem(
        string token,
        long materialId,
        float peso,
        long ecopontoId,
        string qualidade,
        long? agenteId = null)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            bool success = validationResult.success;
            string message = validationResult.message;
            long userId = validationResult.userId;

            if (!success)
            {
                return (false, message, null);
            }

            var (ecopontoSuccess, ecopontoMessage, ecoponto) = await _ecopontoService.ObterEcoponto(ecopontoId);
            if (!ecopontoSuccess || ecoponto == null)
            {
                return (false, "Ecoponto não encontrado", null);
            }

            var (materialSuccess, materialMessage, material) = await _materialService.ObterMaterial(materialId);
            if (!materialSuccess || material == null)
            {
                return (false, "Material não encontrado", null);
            }

            if (agenteId.HasValue)
            {
                var agente = await _supabaseClient.From<Agente>()
                    .Where(a => a.Id == agenteId.Value)
                    .Single();
                if (agente == null)
                {
                    return (false, "Agente não encontrado", null);
                }
            }

            if (peso <= 0)
            {
                return (false, "O peso deve ser maior que zero", null);
            }

            if (!ajustesPontos.ContainsKey(materialId) || !ajustesPontos[materialId].ContainsKey(qualidade))
            {
                return (false, "Material ou qualidade não suportados.", null);
            }

            long ajustePontos = ajustesPontos[materialId][qualidade];
            long pontosBase = (long)(peso * material.Valor);
            long pontosGanhos = pontosBase + ajustePontos;

            var novaReciclagem = new Reciclagem
            {
                CreatedAt = DateTime.UtcNow,
                Peso = peso,
                UsuarioId = userId,
                MaterialId = materialId,
                EcopontoId = ecopontoId,
                AgenteId = agenteId
            };

            var response = await _supabaseClient.From<Reciclagem>()
                .Insert(novaReciclagem);
            var reciclagemInserida = response.Models.FirstOrDefault();

            if (reciclagemInserida == null)
            {
                return (false, "Erro ao inserir reciclagem: não foi possível recuperar o registro inserido", null);
            }

            await _usuarioService.AtualizarPontos(userId, pontosGanhos);

            var participacaoAtiva = await _supabaseClient.From<UsuarioCampanha>()
                .Select("*")
                .Filter("usuario_id", Operator.Equals, Convert.ToInt32(userId))
                .Filter("status", Operator.NotEqual, "Completa")
                .Single();

            if (participacaoAtiva != null)
            {
                Console.WriteLine($"Participação ativa encontrada: CampanhaId={participacaoAtiva.CampanhaId}, UsuarioId={participacaoAtiva.UsuarioId}, Status={participacaoAtiva.Status}");

                try
                {
                    await _supabaseClient.Postgrest.Rpc("inserir_campanha_reciclagem", 
                        new { p_campanha_id = participacaoAtiva.CampanhaId, p_reciclagem_id = reciclagemInserida.Id });

                    Console.WriteLine("Associação CampanhaReciclagem inserida com sucesso via função RPC");

                    if (_campanhaService != null)
                    {
                        var (verifSuccess, verifMessage) = await _campanhaService.VerificarCumprimentoCampanha(userId, participacaoAtiva.CampanhaId);
                        Console.WriteLine($"Resultado da verificação da campanha: Success={verifSuccess}, Message={verifMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao associar reciclagem à campanha via RPC: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Nenhuma participação ativa encontrada para o usuário");
            }

            string? agenteNome = agenteId.HasValue ? (await _supabaseClient.From<Agente>()
                .Where(a => a.Id == agenteId.Value)
                .Single())?.Nome : null;

            var reciclagemDTO = new ReciclagemResponseDTO
            {
                Id = reciclagemInserida.Id,
                CreatedAt = reciclagemInserida.CreatedAt,
                Peso = reciclagemInserida.Peso,
                UsuarioId = reciclagemInserida.UsuarioId,
                MaterialId = reciclagemInserida.MaterialId,
                EcopontoId = reciclagemInserida.EcopontoId,
                AgenteId = reciclagemInserida.AgenteId,
                MaterialNome = material.Nome,
                MaterialClasse = material.Classe,
                PontosGanhos = pontosGanhos,
                EcopontoNome = ecoponto.Nome,
                EcopontoLocalizacao = ecoponto.Localizacao,
                AgenteNome = agenteNome
            };

            return (true, "Reciclagem registrada com sucesso", reciclagemDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao registrar reciclagem: {ex.Message}");
            return (false, $"Erro ao registrar reciclagem: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, AvaliacoesReciclagem? data)> AvaliarReciclagem(string token, int rating, string? comentario)
    {
        try
        {
            // Validar token e obter userId
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }
            long userId = validationResult.userId;

            // Validar rating
            if (rating < 1 || rating > 5)
            {
                return (false, "A nota deve estar entre 1 e 5.", null);
            }

            // Buscar a reciclagem mais recente do usuário logado
            var limiteData = DateTime.UtcNow.AddDays(-1); // Limite de 1 dia atrás
            var reciclagem = await _supabaseClient
                .From<Reciclagem>()
                .Select("*")
                .Where(r => r.UsuarioId == userId)
                .Where(r => r.ColetaId == null) // Não deve estar ligada a uma coleta
                .Where(r => r.CreatedAt >= limiteData) // Criada nas últimas 24 horas
                .Order("created_at", Ordering.Descending) // Mais recente primeiro
                .Limit(1)
                .Single();

            if (reciclagem == null)
            {
                return (false, "Nenhuma reciclagem recente encontrada para avaliação. Certifique-se de ter uma reciclagem nas últimas 24 horas que não esteja ligada a uma coleta.", null);
            }

            // Verificar se já existe uma avaliação para essa reciclagem
            var existingEvaluation = await _supabaseClient
                .From<AvaliacoesReciclagem>()
                .Where(a => a.ReciclagemId == reciclagem.Id)
                .Single();

            if (existingEvaluation != null)
            {
                return (false, "Esta reciclagem já foi avaliada.", null);
            }

            // Obter o agente_id do registro de reciclagem
            if (!reciclagem.AgenteId.HasValue)
            {
                return (false, "Nenhum agente associado a este evento de reciclagem.", null);
            }
            long agenteId = reciclagem.AgenteId.Value;

            // Criar nova avaliação
            var novaAvaliacao = new AvaliacoesReciclagem
            {
                ReciclagemId = reciclagem.Id,
                UsuarioId = userId,
                AgenteId = agenteId,
                Rating = rating,
                Comentario = comentario?.Trim(), // Sanitizar comentário, se presente
                CreatedAt = DateTime.UtcNow
            };

            // Inserir no banco de dados
            var response = await _supabaseClient
                .From<AvaliacoesReciclagem>()
                .Insert(novaAvaliacao);

            var avaliacaoInserida = response.Models.FirstOrDefault();
            if (avaliacaoInserida == null)
            {
                return (false, "Erro ao inserir avaliação: não foi possível recuperar o registro inserido.", null);
            }

            // Retornar sucesso
            return (true, "Avaliação enviada com sucesso.", avaliacaoInserida);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar avaliação: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return (false, $"Erro ao enviar avaliação: {ex.Message}", null);
        }
    }
}