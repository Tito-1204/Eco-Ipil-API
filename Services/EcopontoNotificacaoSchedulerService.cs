using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoIpil.API.Services;

public class EcopontoNotificacaoSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EcopontoNotificacaoSchedulerService> _logger;
    private readonly DayOfWeek[] _diasExecucao = { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }; // Segunda, quarta e sexta

    public EcopontoNotificacaoSchedulerService(IServiceScopeFactory scopeFactory, ILogger<EcopontoNotificacaoSchedulerService> logger)
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

                // Determinar o próximo dia de execução (segunda, quarta ou sexta)
                if (!_diasExecucao.Contains(diaAtual))
                {
                    // Se não for um dia de execução, calcular o próximo dia
                    var diasAteProximo = (_diasExecucao.OrderBy(d => (d - diaAtual + 7) % 7).First() - diaAtual + 7) % 7;
                    if (diasAteProximo == 0) diasAteProximo = 7;
                    proximaExecucao = agora.Date.AddDays(diasAteProximo).AddHours(9); // 9h da manhã UTC
                }
                else
                {
                    // Se for um dia de execução, verificar a hora
                    if (agora.Hour < 9)
                    {
                        // Se for antes das 9h, executar hoje às 9h
                        proximaExecucao = agora.Date.AddHours(9);
                    }
                    else
                    {
                        // Se já passou das 9h, calcular o próximo dia de execução
                        var diasAteProximo = (_diasExecucao.OrderBy(d => (d - diaAtual + 7) % 7).First() - diaAtual + 7) % 7;
                        if (diasAteProximo == 0) diasAteProximo = 7;
                        proximaExecucao = agora.Date.AddDays(diasAteProximo).AddHours(9); // Próxima execução
                    }
                }

                // Calcular o tempo de espera
                var delay = proximaExecucao - DateTime.UtcNow;
                if (delay.TotalMilliseconds <= 0)
                {
                    // Se o tempo de espera for negativo ou zero, esperar 1 minuto para evitar loop infinito
                    delay = TimeSpan.FromMinutes(1);
                }

                // Logar a próxima execução
                _logger.LogInformation("Próxima verificação de notificações de ecopontos será em {ProximaExecucao}", proximaExecucao);

                // Aguardar o tempo calculado
                await Task.Delay(delay, stoppingToken);

                // Executar a verificação de ecopontos
                _logger.LogInformation("Executando verificação de notificações de ecopontos...");
                using (var scope = _scopeFactory.CreateScope())
                {
                    var ecopontoService = scope.ServiceProvider.GetRequiredService<EcopontoService>();
                    var notificacaoService = scope.ServiceProvider.GetRequiredService<NotificacaoService>();

                    // Listar ecopontos criados nos últimos 5 dias
                    var dataAtual = DateTime.UtcNow;
                    var ecopontos = (await ecopontoService.ListarEcopontos()).ecopontos;
                    var novosEcopontos = ecopontos?.Where(e => (dataAtual - e.CreatedAt).TotalDays <= 5).ToList();

                    if (novosEcopontos != null && novosEcopontos.Any())
                    {
                        foreach (var ecoponto in novosEcopontos)
                        {
                            var mensagem = $"Novo ecoponto disponível: {ecoponto.Nome}! Localização: {ecoponto.Localizacao}. Recicle e contribua para um futuro sustentável!";
                            var (success, message) = await notificacaoService.CriarNotificacaoGeral(
                                mensagem: mensagem,
                                tipo: "Novo Ecoponto",
                                dataExpiracao: null
                            );

                            if (success)
                            {
                                _logger.LogInformation("Notificação geral criada para novo ecoponto {EcopontoId}", ecoponto.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Falha ao criar notificação para ecoponto {EcopontoId}: {Message}", ecoponto.Id, message);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Nenhum ecoponto novo encontrado para notificação.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar a verificação de notificações de ecopontos");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Tenta novamente em 1 hora
            }
        }
    }
}