using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using EcoIpil.API.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Supabase.Postgrest.Models;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;
using System.Net;

namespace EcoIpil.API.Services
{
    public class CampanhaService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly ILogger<CampanhaService> _logger;
        private readonly UsuarioService _usuarioService;
        private readonly IConfiguration _configuration;
        private readonly NotificacaoService _notificacaoService;

        public CampanhaService(SupabaseService supabaseService, ILogger<CampanhaService> logger, UsuarioService usuarioService, IConfiguration configuration, NotificacaoService notificacaoService)
        {
            _supabaseClient = supabaseService.GetClient();
            _logger = logger;
            _usuarioService = usuarioService;
            _configuration = configuration;
            _notificacaoService = notificacaoService;
        }

        private async Task<bool> VerificarConectividade()
        {
            try
            {
                var url = _configuration["Supabase:Url"];
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("URL do Supabase não configurada");
                    return false;
                }

                Uri uri = new Uri(url);
                string host = uri.Host;

                try
                {
                    IPHostEntry entry = await Dns.GetHostEntryAsync(host);
                    if (entry != null && entry.AddressList.Length > 0)
                    {
                        _logger.LogInformation($"Host {host} resolvido com sucesso");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Não foi possível resolver o host {host}");
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar conectividade");
                return false;
            }
        }

        public async Task<(bool Success, string Message, List<CampanhaResponseDTO> Campanhas)> ListarCampanhasAtivas(long? usuarioId = null)
        {
            try
            {
                _logger.LogInformation("Listando campanhas ativas");

                bool conectividade = await VerificarConectividade();
                if (!conectividade)
                {
                    return (false, "Não foi possível conectar ao servidor de dados. Verifique sua conexão com a internet.", new List<CampanhaResponseDTO>());
                }

                var query = _supabaseClient.From<Campanha>().Select("*")
                    .Filter("status", Operator.Equals, "Ativo");

                var dataAtual = DateTime.UtcNow.ToString("o");
                query = query.Filter("data_inicio", Operator.LessThanOrEqual, dataAtual)
                             .Filter("data_fim", Operator.GreaterThanOrEqual, dataAtual);

                var campanhas = await query.Get();

                if (campanhas.Models.Count == 0)
                {
                    return (true, "Nenhuma campanha ativa encontrada", new List<CampanhaResponseDTO>());
                }

                var result = new List<CampanhaResponseDTO>();

                if (usuarioId.HasValue)
                {
                    var usuarioCampanhas = await _supabaseClient.From<UsuarioCampanha>().Select("*")
                        .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId.Value))
                        .Get();

                    var participacaoStatus = new Dictionary<int, string>();
                    foreach (var uc in usuarioCampanhas.Models)
                    {
                        participacaoStatus[Convert.ToInt32(uc.CampanhaId)] = uc.Status;
                    }

                    foreach (var campanha in campanhas.Models)
                    {
                        var dto = new CampanhaResponseDTO
                        {
                            Id = campanha.Id,
                            Titulo = campanha.Titulo,
                            Descricao = campanha.Descricao,
                            Status = campanha.Status,
                            DataInicio = campanha.DataInicio,
                            DataFim = campanha.DataFim,
                            Pontos = campanha.Pontos,
                            ParticipacaoStatus = participacaoStatus.ContainsKey(Convert.ToInt32(campanha.Id))
                                ? participacaoStatus[Convert.ToInt32(campanha.Id)]
                                : "Não Participando"
                        };

                        result.Add(dto);
                    }
                }
                else
                {
                    foreach (var campanha in campanhas.Models)
                    {
                        var dto = new CampanhaResponseDTO
                        {
                            Id = campanha.Id,
                            Titulo = campanha.Titulo,
                            Descricao = campanha.Descricao,
                            Status = campanha.Status,
                            DataInicio = campanha.DataInicio,
                            DataFim = campanha.DataFim,
                            Pontos = campanha.Pontos,
                            ParticipacaoStatus = "Não Participando"
                        };
                        result.Add(dto);
                    }
                }

                return (true, "Campanhas ativas recuperadas com sucesso", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar campanhas ativas");
                return (false, $"Erro ao buscar campanhas: {ex.Message}", new List<CampanhaResponseDTO>());
            }
        }

        public async Task<(bool Success, string Message, CampanhaResponseDTO Campanha)> ObterCampanha(long campanhaId, long? usuarioId = null)
        {
            try
            {
                _logger.LogInformation($"Obtendo detalhes da campanha ID: {campanhaId}");

                bool conectividade = await VerificarConectividade();
                if (!conectividade)
                {
                    return (false, "Não foi possível conectar ao servidor de dados. Verifique sua conexão com a internet.", new CampanhaResponseDTO());
                }

                var campanha = await _supabaseClient.From<Campanha>().Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (campanha == null)
                {
                    return (false, "Campanha não encontrada", new CampanhaResponseDTO());
                }

                var dto = new CampanhaResponseDTO
                {
                    Id = campanha.Id,
                    Titulo = campanha.Titulo,
                    Descricao = campanha.Descricao,
                    Status = campanha.Status,
                    DataInicio = campanha.DataInicio,
                    DataFim = campanha.DataFim,
                    Pontos = campanha.Pontos
                };

                if (usuarioId.HasValue)
                {
                    var usuarioCampanha = await _supabaseClient.From<UsuarioCampanha>().Select("*")
                        .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId.Value))
                        .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                        .Single();

                    dto.ParticipacaoStatus = usuarioCampanha != null ? usuarioCampanha.Status : "Não Participando";
                }

                return (true, "Campanha encontrada com sucesso", dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter campanha ID: {campanhaId}");
                return (false, $"Erro ao buscar campanha: {ex.Message}", new CampanhaResponseDTO());
            }
        }

        public async Task<(bool Success, string Message)> ParticiparCampanha(long campanhaId, long usuarioId)
        {
            try
            {
                _logger.LogInformation($"Registrando participação do usuário {usuarioId} na campanha {campanhaId}");

                // Verificar se o usuário já está participando de outra campanha ativa
                var participacoesAtivas = await _supabaseClient.From<UsuarioCampanha>()
                    .Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Filter("status", Operator.NotEqual, "Completa")
                    .Get();

                if (participacoesAtivas.Models.Any())
                {
                    return (false, "Você já está participando de outra campanha. Complete ou aguarde o término da campanha atual para se inscrever em uma nova.");
                }

                var campanha = await _supabaseClient.From<Campanha>().Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (campanha == null)
                {
                    return (false, "Campanha não encontrada");
                }

                if (campanha.Status != "Ativo")
                {
                    return (false, "Esta campanha não está ativa");
                }

                var dataAtual = DateTime.UtcNow;
                if (dataAtual < campanha.DataInicio || dataAtual > campanha.DataFim)
                {
                    return (false, "Esta campanha está fora do período válido");
                }

                var participacaoExistente = await _supabaseClient.From<UsuarioCampanha>().Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (participacaoExistente != null)
                {
                    return (false, $"Usuário já está participando desta campanha com status: {participacaoExistente.Status}");
                }

                var usuarioElegivel = await VerificarElegibilidadeUsuario(usuarioId, campanhaId);
                if (!usuarioElegivel.Elegivel)
                {
                    return (false, $"Usuário não elegível para participar: {usuarioElegivel.Motivo}");
                }

                var novaParticipacao = new UsuarioCampanha
                {
                    UsuarioId = usuarioId,
                    CampanhaId = campanhaId,
                    Status = "Pendente"
                };

                await _supabaseClient.From<UsuarioCampanha>().Insert(novaParticipacao);

                return (true, "Participação registrada com sucesso. Status: Pendente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar participação na campanha {campanhaId}");
                return (false, $"Erro ao registrar participação: {ex.Message}");
            }
        }

        private async Task<(bool Elegivel, string Motivo)> VerificarElegibilidadeUsuario(long usuarioId, long campanhaId)
        {
            try
            {
                var usuario = await _supabaseClient.From<Usuario>().Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Single();

                if (usuario == null)
                {
                    return (false, "Usuário não encontrado ou inativo");
                }

                if (usuario.Status != "Ativo")
                {
                    return (false, "Usuário não está ativo");
                }

                if (usuario.PontosTotais < 100)
                {
                    return (false, "Usuário precisa ter no mínimo 100 pontos para participar");
                }

                var historicoReciclagem = await _supabaseClient.From<Reciclagem>().Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Get();

                if (historicoReciclagem.Models.Count < 5)
                {
                    return (false, "Usuário precisa ter feito no mínimo 5 reciclagens para participar");
                }

                return (true, "Usuário elegível para participar");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar elegibilidade do usuário {usuarioId}");
                return (false, $"Erro ao verificar elegibilidade: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> VerificarCumprimentoCampanha(long usuarioId, long campanhaId)
        {
            try
            {
                _logger.LogInformation($"Verificando cumprimento da campanha {campanhaId} pelo usuário {usuarioId}");

                var participacao = await _supabaseClient.From<UsuarioCampanha>()
                    .Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (participacao == null)
                {
                    return (false, "Usuário não está participando desta campanha");
                }

                if (participacao.Status == "Completa")
                {
                    return (true, "Usuário já completou esta campanha");
                }

                var campanha = await _supabaseClient.From<Campanha>()
                    .Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (campanha == null)
                {
                    return (false, "Campanha não encontrada");
                }

                var requisitos = ExtrairRequisitos(campanha.Descricao);
                if (string.IsNullOrEmpty(requisitos.Material) || requisitos.Quantidade == 0)
                {
                    return (false, "Não foi possível determinar os requisitos da campanha a partir da descrição");
                }

                var material = await _supabaseClient.From<Material>()
                    .Select("*")
                    .Filter("nome", Operator.Equals, requisitos.Material)
                    .Single();

                if (material == null)
                {
                    return (false, $"Material {requisitos.Material} não encontrado na base de dados");
                }

                var reciclagens = await _supabaseClient.From<Reciclagem>()
                    .Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(usuarioId))
                    .Filter("material_id", Operator.Equals, Convert.ToInt32(material.Id))
                    .Get();

                var reciclagensIds = reciclagens.Models.Select(r => r.Id).ToList();
                var campanhasReciclagem = await _supabaseClient.From<CampanhaReciclagem>()
                    .Select("*")
                    .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Filter("reciclagem_id", Operator.In, reciclagensIds)
                    .Get();

                float pesoTotal = campanhasReciclagem.Models
                    .Select(cr => reciclagens.Models.First(r => r.Id == cr.ReciclagemId).Peso)
                    .Sum();

                bool requisitosAtendidos = pesoTotal >= requisitos.Quantidade;

                if (requisitosAtendidos)
                {
                    participacao.Status = "Completa";
                    await _supabaseClient.From<UsuarioCampanha>().Update(participacao);
                    await _usuarioService.AtualizarPontos(usuarioId, campanha.Pontos);

                    var mensagem = $"Parabéns! Você completou a campanha '{campanha.Titulo}' e ganhou {campanha.Pontos} pontos!";
                    await _notificacaoService.CriarNotificacaoPessoal(usuarioId, mensagem, "Campanha Completa", DateTime.UtcNow.AddDays(7));

                    return (true, $"Campanha concluída com sucesso! {campanha.Pontos} pontos adicionados.");
                }

                return (true, "Campanha ainda em andamento, requisitos não foram totalmente atendidos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar cumprimento da campanha {campanhaId}");
                return (false, $"Erro ao verificar cumprimento: {ex.Message}");
            }
        }


        public async Task<(bool Success, string Message)> RegistrarReciclagemCampanha(long reciclagemId, long campanhaId)
        {
            try
            {
                _logger.LogInformation($"Registrando reciclagem {reciclagemId} para a campanha {campanhaId}");

                var reciclagem = await _supabaseClient.From<Reciclagem>().Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(reciclagemId))
                    .Single();

                if (reciclagem == null)
                {
                    return (false, "Reciclagem não encontrada");
                }

                var campanha = await _supabaseClient.From<Campanha>().Select("*")
                    .Filter("id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (campanha == null)
                {
                    return (false, "Campanha não encontrada");
                }

                if (campanha.Status != "Ativo")
                {
                    return (false, "Esta campanha não está ativa");
                }

                if (reciclagem.CreatedAt < campanha.DataInicio || reciclagem.CreatedAt > campanha.DataFim)
                {
                    return (false, "A data da reciclagem está fora do período da campanha");
                }

                var participacao = await _supabaseClient.From<UsuarioCampanha>().Select("*")
                    .Filter("usuario_id", Operator.Equals, Convert.ToInt32(reciclagem.UsuarioId))
                    .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Single();

                if (participacao == null)
                {
                    return (false, "O usuário não está participando desta campanha");
                }

                var campanhaReciclagemExistente = await _supabaseClient.From<CampanhaReciclagem>().Select("*")
                    .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanhaId))
                    .Filter("reciclagem_id", Operator.Equals, Convert.ToInt32(reciclagemId))
                    .Single();

                if (campanhaReciclagemExistente != null)
                {
                    return (false, "Esta reciclagem já foi registrada para esta campanha");
                }

                var campanhaReciclagem = new CampanhaReciclagem
                {
                    CampanhaId = campanhaId,
                    ReciclagemId = reciclagemId
                };

                await _supabaseClient.From<CampanhaReciclagem>().Insert(campanhaReciclagem);

                await VerificarCumprimentoCampanha(reciclagem.UsuarioId, campanhaId);

                return (true, "Reciclagem registrada para a campanha com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar reciclagem {reciclagemId} para a campanha {campanhaId}");
                return (false, $"Erro ao registrar reciclagem: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CriarNotificacoesIniciandoCampanhas()
        {
            try
            {
                _logger.LogInformation("Verificando campanhas que iniciam hoje para criar notificações gerais");

                // Obter a data atual no formato UTC
                var dataAtual = DateTime.UtcNow.Date;

                // Buscar campanhas que começam hoje
                var campanhasIniciando = await _supabaseClient.From<Campanha>()
                    .Select("*")
                    .Filter("data_inicio", Operator.Equals, dataAtual.ToString("o"))
                    .Get();

                if (campanhasIniciando.Models == null || !campanhasIniciando.Models.Any())
                {
                    _logger.LogInformation("Nenhuma campanha inicia hoje");
                    return (true, "Nenhuma campanha inicia hoje para criar notificações");
                }

                // Para cada campanha que inicia hoje, criar uma notificação geral
                foreach (var campanha in campanhasIniciando.Models)
                {
                    var mensagem = $"Nova campanha iniciada: {campanha.Titulo}! Participe até {campanha.DataFim:dd/MM/yyyy} e ganhe {campanha.Pontos} pontos.";
                    var (success, message) = await _notificacaoService.CriarNotificacaoGeral(
                        mensagem: mensagem,
                        tipo: "Campanha Iniciada",
                        dataExpiracao: campanha.DataFim
                    );

                    if (!success)
                    {
                        _logger.LogWarning("Falha ao criar notificação geral para a campanha {CampanhaId}: {Message}", campanha.Id, message);
                    }
                    else
                    {
                        _logger.LogInformation("Notificação geral criada para a campanha {CampanhaId}", campanha.Id);
                    }
                }

                return (true, "Notificações gerais criadas para campanhas iniciando hoje");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar notificações para campanhas iniciando");
                return (false, $"Erro ao criar notificações: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CriarNotificacoesCampanhasEncerrando(int diasRestantes = 3)
        {
            try
            {
                _logger.LogInformation("Verificando campanhas que estão prestes a encerrar para criar notificações pessoais");

                // Obter a data atual e a data limite (data atual + diasRestantes)
                var dataAtual = DateTime.UtcNow.Date;
                var dataLimite = dataAtual.AddDays(diasRestantes).Date;

                // Buscar campanhas que estão ativas e terminam dentro do intervalo especificado
                var campanhasEncerrando = await _supabaseClient.From<Campanha>()
                    .Select("*")
                    .Filter("status", Operator.Equals, "Ativo")
                    .Filter("data_fim", Operator.GreaterThanOrEqual, dataAtual.ToString("o"))
                    .Filter("data_fim", Operator.LessThanOrEqual, dataLimite.ToString("o"))
                    .Get();

                if (campanhasEncerrando.Models == null || !campanhasEncerrando.Models.Any())
                {
                    _logger.LogInformation("Nenhuma campanha está prestes a encerrar nos próximos {DiasRestantes} dias", diasRestantes);
                    return (true, "Nenhuma campanha está prestes a encerrar para criar notificações");
                }

                // Para cada campanha que está prestes a encerrar
                foreach (var campanha in campanhasEncerrando.Models)
                {
                    // Buscar usuários que estão participando dessa campanha e ainda não a completaram
                    var participacoes = await _supabaseClient.From<UsuarioCampanha>()
                        .Select("*")
                        .Filter("campanha_id", Operator.Equals, Convert.ToInt32(campanha.Id))
                        .Filter("status", Operator.NotEqual, "Completa")
                        .Get();

                    if (participacoes.Models == null || !participacoes.Models.Any())
                    {
                        _logger.LogInformation("Nenhum usuário pendente encontrado para a campanha {CampanhaId}", campanha.Id);
                        continue;
                    }

                    // Criar uma notificação pessoal para cada usuário pendente
                    foreach (var participacao in participacoes.Models)
                    {
                        var mensagem = $"A campanha {campanha.Titulo} está quase acabando! Você tem até {campanha.DataFim:dd/MM/yyyy} para completá-la e ganhar {campanha.Pontos} pontos.";
                        var (success, message) = await _notificacaoService.CriarNotificacaoPessoal(
                            usuarioId: participacao.UsuarioId,
                            mensagem: mensagem,
                            tipo: "Campanha Encerrando",
                            dataExpiracao: campanha.DataFim
                        );

                        if (!success)
                        {
                            _logger.LogWarning("Falha ao criar notificação pessoal para o usuário {UsuarioId} na campanha {CampanhaId}: {Message}", participacao.UsuarioId, campanha.Id, message);
                        }
                        else
                        {
                            _logger.LogInformation("Notificação pessoal criada para o usuário {UsuarioId} na campanha {CampanhaId}", participacao.UsuarioId, campanha.Id);
                        }
                    }
                }

                return (true, "Notificações pessoais criadas para campanhas prestes a encerrar");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar notificações para campanhas encerrando");
                return (false, $"Erro ao criar notificações: {ex.Message}");
            }
        }

        private (string Material, string TipoRequisito, float Quantidade, float? PesoMinimoPorReciclagem) ExtrairRequisitos(string descricao)
        {
            var materiais = new[] { "Metal", "Plástico", "Papel", "Vidro" };
            string materialEncontrado = materiais.FirstOrDefault(m => descricao.Contains(m, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

            if (string.IsNullOrEmpty(materialEncontrado))
            {
                return (string.Empty, string.Empty, 0, null);
            }

            // Procurar por "recicle X kg"
            var regexPeso = new System.Text.RegularExpressions.Regex(@"recicle (\d+\.?\d*)\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchPeso = regexPeso.Match(descricao);
            if (matchPeso.Success)
            {
                float quantidade = float.Parse(matchPeso.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                return (materialEncontrado, "PesoTotal", quantidade, null);
            }

            // Procurar por "recicle X vezes" com opcional "cada vez com pelo menos Y kg"
            var regexVezes = new System.Text.RegularExpressions.Regex(@"recicle (\d+)\s*vezes", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchVezes = regexVezes.Match(descricao);
            if (matchVezes.Success)
            {
                int vezes = int.Parse(matchVezes.Groups[1].Value);
                float? pesoMinimo = null;
                var regexPesoMinimo = new System.Text.RegularExpressions.Regex(@"cada vez com pelo menos (\d+\.?\d*)\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matchPesoMinimo = regexPesoMinimo.Match(descricao);
                if (matchPesoMinimo.Success)
                {
                    pesoMinimo = float.Parse(matchPesoMinimo.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }
                return (materialEncontrado, "NumeroReciclagens", vezes, pesoMinimo);
            }

            return (string.Empty, string.Empty, 0, null);
        }

    }
}