using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EcoIpil.API.Models;
using System.Threading.Tasks;
using Supabase.Postgrest.Models;

namespace EcoIpil.API.Services;

public class InvestimentoNotificacaoSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvestimentoNotificacaoSchedulerService> _logger;
    private readonly TimeSpan _intervaloSemanal = TimeSpan.FromDays(3.5); // Aproximadamente 2 vezes por semana
    private readonly DayOfWeek[] _diasExecucao = { DayOfWeek.Monday, DayOfWeek.Thursday }; // Segunda e quinta
    private HashSet<long> _investimentosNotificados = new HashSet<long>(); // Rastrear investimentos já notificados (notificações gerais)

    public InvestimentoNotificacaoSchedulerService(IServiceScopeFactory scopeFactory, ILogger<InvestimentoNotificacaoSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var agora = DateTime.UtcNow;
                var diaAtual = agora.DayOfWeek;
                var proximaExecucao = agora;

                // Determinar o próximo dia de execução (segunda ou quinta, às 9h)
                if (!_diasExecucao.Contains(diaAtual))
                {
                    var diasAteProximo = (_diasExecucao.OrderBy(d => (d - diaAtual + 7) % 7).First() - diaAtual + 7) % 7;
                    if (diasAteProximo == 0) diasAteProximo = 7;
                    proximaExecucao = agora.Date.AddDays(diasAteProximo).AddHours(9); // 9h da manhã UTC
                }
                else
                {
                    if (agora.Hour < 9)
                    {
                        proximaExecucao = agora.Date.AddHours(9); // Executar hoje às 9h
                    }
                    else
                    {
                        var diasAteProximo = (_diasExecucao.OrderBy(d => (d - diaAtual + 7) % 7).Skip(1).FirstOrDefault() - diaAtual + 7) % 7;
                        if (diasAteProximo == 0) diasAteProximo = 7;
                        proximaExecucao = agora.Date.AddDays(diasAteProximo).AddHours(9); // Próxima execução
                    }
                }

                var delay = proximaExecucao - agora;
                if (delay.TotalMilliseconds <= 0) delay = TimeSpan.FromMinutes(1);

                _logger.LogInformation("Próxima verificação de notificações de investimentos será em {ProximaExecucao}", proximaExecucao);
                await Task.Delay(delay, stoppingToken);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var investimentoService = scope.ServiceProvider.GetRequiredService<InvestimentoService>();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<UsuarioService>();
                    var notificacaoService = scope.ServiceProvider.GetRequiredService<NotificacaoService>();
                    var supabaseClient = scope.ServiceProvider.GetRequiredService<SupabaseService>().GetClient();

                    var investimentosAtivos = await investimentoService.GetActiveInvestments();
                    var usuarios = await supabaseClient.From<Usuario>().Select("*").Get();

                    foreach (var investimento in investimentosAtivos)
                    {
                        var semanaCriacao = (agora - investimento.CreatedAt).TotalDays <= 7;

                        // Notificação geral para novos investimentos
                        if (!_investimentosNotificados.Contains(investimento.Id) && semanaCriacao)
                        {
                            var mensagemGeral = $"Novo investimento disponível: {investimento.Nome}! Invista seus pontos para lucrar em 6 meses ou 1 ano.";
                            var (successGeral, messageGeral) = await notificacaoService.CriarNotificacaoGeral(
                                mensagem: mensagemGeral,
                                tipo: "Novo Investimento",
                                dataExpiracao: null
                            );

                            if (successGeral)
                            {
                                _investimentosNotificados.Add(investimento.Id);
                                _logger.LogInformation("Notificação geral enviada para novo investimento {InvestimentoId}", investimento.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Falha ao criar notificação geral para investimento {InvestimentoId}: {Message}", investimento.Id, messageGeral);
                            }
                        }

                        // Notificações pessoais para usuários elegíveis
                        foreach (var usuario in usuarios.Models)
                        {
                            var (successCarteira, _, pontos) = await usuarioService.GetUserWallet(usuario.Id);
                            if (!successCarteira || pontos < 2000) continue;

                            var jaInvestiu = (await supabaseClient.From<Investir>()
                                .Where(i => i.UsuarioId == usuario.Id && i.InvestimentoId == investimento.Id)
                                .Get()).Models.Any();

                            if (jaInvestiu || investimento.TotalInvestido >= investimento.Meta) continue;

                            // Verificar se a notificação já foi enviada para este usuário e investimento
                            var notificacaoJaEnviada = (await supabaseClient.From<NotificacaoInvestimento>()
                                .Where(ni => ni.UsuarioId == usuario.Id && ni.InvestimentoId == investimento.Id && ni.Tipo == "Convite Investimento")
                                .Get()).Models.Any();

                            if (notificacaoJaEnviada) continue;

                            var mensagem = semanaCriacao
                                ? $"Novo investimento '{investimento.Nome}' disponível! Invista seus {pontos} pontos para lucrar até 55% em 6 meses ou 1 ano."
                                : $"Invista em '{investimento.Nome}'! Seus {pontos} pontos podem render lucros em 6 meses ou 1 ano.";
                            var (success, message) = await notificacaoService.CriarNotificacaoPessoal(
                                usuarioId: usuario.Id,
                                mensagem: mensagem,
                                tipo: "Convite Investimento",
                                dataExpiracao: DateTime.UtcNow.AddDays(7)
                            );

                            if (success)
                            {
                                // Registrar a notificação enviada
                                await supabaseClient.From<NotificacaoInvestimento>().Insert(new NotificacaoInvestimento
                                {
                                    UsuarioId = usuario.Id,
                                    InvestimentoId = investimento.Id,
                                    Tipo = "Convite Investimento",
                                    DataEnvio = DateTime.UtcNow
                                });

                                _logger.LogInformation("Notificação de investimento enviada para usuário {UsuarioId}", usuario.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Falha ao criar notificação para usuário {UsuarioId}: {Message}", usuario.Id, message);
                            }
                        }

                        // Três notificações na semana de criação (adicionar quarta-feira)
                        if (semanaCriacao && diaAtual == DayOfWeek.Wednesday)
                        {
                            foreach (var usuario in usuarios.Models)
                            {
                                var (successCarteira, _, pontos) = await usuarioService.GetUserWallet(usuario.Id);
                                if (!successCarteira || pontos < 2000) continue;

                                var jaInvestiu = (await supabaseClient.From<Investir>()
                                    .Where(i => i.UsuarioId == usuario.Id && i.InvestimentoId == investimento.Id)
                                    .Get()).Models.Any();

                                if (jaInvestiu || investimento.TotalInvestido >= investimento.Meta) continue;

                                // Verificar se a notificação de "última chance" já foi enviada
                                var ultimaChanceEnviada = (await supabaseClient.From<NotificacaoInvestimento>()
                                    .Where(ni => ni.UsuarioId == usuario.Id && ni.InvestimentoId == investimento.Id && ni.Tipo == "Ultima Chance Investimento")
                                    .Get()).Models.Any();

                                if (ultimaChanceEnviada) continue;

                                var mensagem = $"Última chance esta semana! Invista em '{investimento.Nome}' e lucre com seus {pontos} pontos.";
                                var (success, message) = await notificacaoService.CriarNotificacaoPessoal(
                                    usuarioId: usuario.Id,
                                    mensagem: mensagem,
                                    tipo: "Ultima Chance Investimento",
                                    dataExpiracao: DateTime.UtcNow.AddDays(7)
                                );

                                if (success)
                                {
                                    // Registrar a notificação de última chance
                                    await supabaseClient.From<NotificacaoInvestimento>().Insert(new NotificacaoInvestimento
                                    {
                                        UsuarioId = usuario.Id,
                                        InvestimentoId = investimento.Id,
                                        Tipo = "Ultima Chance Investimento",
                                        DataEnvio = DateTime.UtcNow
                                    });

                                    _logger.LogInformation("Terceira notificação semanal enviada para usuário {UsuarioId}", usuario.Id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar a verificação de notificações de investimentos");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Tenta novamente em 1 hora
            }
        }
    }
}
