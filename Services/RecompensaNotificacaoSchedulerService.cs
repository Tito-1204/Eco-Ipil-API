using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using EcoIpil.API.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EcoIpil.API.Services;

public class RecompensaNotificacaoSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecompensaNotificacaoSchedulerService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromDays(1); // Executar a cada 24 horas
    private readonly TimeSpan _horaExecucao = TimeSpan.FromHours(8); // 8h da manhã UTC

    public RecompensaNotificacaoSchedulerService(IServiceScopeFactory scopeFactory, ILogger<RecompensaNotificacaoSchedulerService> logger)
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
                // Calcular o tempo até a próxima execução (8h da manhã)
                var dataAtual = DateTime.UtcNow;
                var proximaExecucao = dataAtual.Date.AddDays(1).Add(_horaExecucao);
                var delay = proximaExecucao - dataAtual;

                if (delay.TotalMilliseconds < 0)
                {
                    proximaExecucao = proximaExecucao.AddDays(1);
                    delay = proximaExecucao - dataAtual;
                }

                _logger.LogInformation("Próxima verificação de notificações de recompensas será em {ProximaExecucao}", proximaExecucao);
                await Task.Delay(delay, stoppingToken);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var recompensaService = scope.ServiceProvider.GetRequiredService<RecompensaService>();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<UsuarioService>();
                    var notificacaoService = scope.ServiceProvider.GetRequiredService<NotificacaoService>();
                    var supabaseClient = scope.ServiceProvider.GetRequiredService<SupabaseService>().GetClient();

                    // Listar recompensas criadas nas últimas 24 horas (sem autenticação)
                    var dataVerificacao = DateTime.UtcNow;
                    var recompensas = (await recompensaService.ListarRecompensasSemAutenticacao()).recompensas;
                    var novasRecompensas = recompensas?.Where(r => (dataVerificacao - r.CreatedAt).TotalHours <= 24).ToList();

                    if (novasRecompensas != null && novasRecompensas.Any())
                    {
                        // Obter todos os usuários
                        var usuarios = await supabaseClient.From<Usuario>().Select("*").Get();

                        foreach (var recompensa in novasRecompensas)
                        {
                            foreach (var usuario in usuarios.Models)
                            {
                                // Verificar se o usuário já resgatou essa recompensa
                                var jaResgatou = (await supabaseClient.From<RecompensaUsuario>()
                                    .Where(ru => ru.UsuarioId == usuario.Id && ru.RecompensaId == recompensa.Id)
                                    .Get()).Models.Any();

                                if (jaResgatou || recompensa.QtRestante <= 0) continue;

                                var mensagem = $"🎉 Nova recompensa disponível: {recompensa.Nome}! Custa apenas {recompensa.Pontos} pontos. Resgate agora e aproveite essa oferta incrível!";
                                var (success, message) = await notificacaoService.CriarNotificacaoPessoal(
                                    usuarioId: usuario.Id,
                                    mensagem: mensagem,
                                    tipo: "Nova Recompensa",
                                    dataExpiracao: DateTime.UtcNow.AddDays(7)
                                );

                                if (success)
                                {
                                    _logger.LogInformation("Notificação criada para usuário {UsuarioId} sobre nova recompensa {RecompensaId}", usuario.Id, recompensa.Id);
                                }
                                else
                                {
                                    _logger.LogWarning("Falha ao criar notificação para usuário {UsuarioId}: {Message}", usuario.Id, message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar a verificação de notificações de recompensas");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Tenta novamente em 1 hora
            }
        }
    }
}