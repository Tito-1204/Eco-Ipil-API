using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using EcoIpil.API.Models;
using System.Threading.Tasks;
using Supabase;

namespace EcoIpil.API.Services;

public class TicketExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketExpirationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Verifica a cada hora

    public TicketExpirationService(IServiceScopeFactory scopeFactory, ILogger<TicketExpirationService> logger)
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
                _logger.LogInformation("Verificando tickets expirados...");
                using (var scope = _scopeFactory.CreateScope())
                {
                    var supabaseClient = scope.ServiceProvider.GetRequiredService<SupabaseService>().GetClient();

                    // Buscar tickets que não estão expirados e cuja data de validade passou
                    var tickets = await supabaseClient
                        .From<Ticket>()
                        .Where(t => t.Status != "Expirado" && t.DataValidade < DateTime.UtcNow)
                        .Get();

                    if (tickets.Models.Any())
                    {
                        foreach (var ticket in tickets.Models)
                        {
                            ticket.Status = "Expirado";
                            await supabaseClient
                                .From<Ticket>()
                                .Where(t => t.Id == ticket.Id)
                                .Update(ticket);

                            _logger.LogInformation("Ticket {TicketId} marcado como Expirado.", ticket.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Nenhum ticket expirado encontrado.");
                    }
                }

                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar tickets expirados.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Tenta novamente em 10 minutos
            }
        }
    }
}