using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoIpil.API.Services;

public class NotificacaoSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificacaoSchedulerService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromDays(1); // Executar a cada 24 horas
    private readonly TimeSpan _horaExecucao = TimeSpan.FromHours(0); // Meia-noite (00:00)

    public NotificacaoSchedulerService(IServiceScopeFactory scopeFactory, ILogger<NotificacaoSchedulerService> logger)
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
                // Calcular o tempo até a próxima execução (próxima meia-noite)
                var agora = DateTime.UtcNow;
                var proximaExecucao = agora.Date.AddDays(1).Add(_horaExecucao);
                var delay = proximaExecucao - agora;

                if (delay.TotalMilliseconds < 0)
                {
                    // Se já passou da meia-noite, agendar para a próxima meia-noite
                    proximaExecucao = proximaExecucao.AddDays(1);
                    delay = proximaExecucao - agora;
                }

                _logger.LogInformation("Próxima verificação de notificações será em {ProximaExecucao}", proximaExecucao);
                await Task.Delay(delay, stoppingToken);

                // Criar um escopo temporário para resolver o CampanhaService
                using (var scope = _scopeFactory.CreateScope())
                {
                    var campanhaService = scope.ServiceProvider.GetRequiredService<CampanhaService>();

                    // Executar os métodos de criação de notificações
                    _logger.LogInformation("Executando verificação de notificações para campanhas");

                    // Criar notificações para campanhas que iniciam hoje
                    var (successInicio, messageInicio) = await campanhaService.CriarNotificacoesIniciandoCampanhas();
                    if (successInicio)
                    {
                        _logger.LogInformation("Notificações de campanhas iniciando: {Message}", messageInicio);
                    }
                    else
                    {
                        _logger.LogError("Erro ao criar notificações de campanhas iniciando: {Message}", messageInicio);
                    }

                    // Criar notificações para campanhas que estão prestes a encerrar
                    var (successEncerrando, messageEncerrando) = await campanhaService.CriarNotificacoesCampanhasEncerrando();
                    if (successEncerrando)
                    {
                        _logger.LogInformation("Notificações de campanhas encerrando: {Message}", messageEncerrando);
                    }
                    else
                    {
                        _logger.LogError("Erro ao criar notificações de campanhas encerrando: {Message}", messageEncerrando);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar a verificação de notificações");
                // Aguardar o intervalo antes de tentar novamente
                await Task.Delay(_intervalo, stoppingToken);
            }
        }
    }
}