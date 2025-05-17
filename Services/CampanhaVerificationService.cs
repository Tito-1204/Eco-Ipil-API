using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services
{
    public class CampanhaVerificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Supabase.Client _supabaseClient;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public CampanhaVerificationService(IServiceScopeFactory scopeFactory, SupabaseService supabaseService)
        {
            _scopeFactory = scopeFactory;
            _supabaseClient = supabaseService.GetClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"[{DateTime.UtcNow}] Iniciando verificação de campanhas pendentes...");
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var campanhaService = scope.ServiceProvider.GetRequiredService<CampanhaService>();
                        var participacoesPendentes = await _supabaseClient.From<UsuarioCampanha>()
                            .Select("*")
                            .Filter("status", Operator.Equals, "Pendente")
                            .Get();

                        Console.WriteLine($"[{DateTime.UtcNow}] Encontradas {participacoesPendentes.Models.Count} participações pendentes.");
                        foreach (var participacao in participacoesPendentes.Models)
                        {
                            Console.WriteLine($"[{DateTime.UtcNow}] Verificando campanha {participacao.CampanhaId} para o usuário {participacao.UsuarioId}...");
                            await campanhaService.VerificarCumprimentoCampanha(participacao.UsuarioId, participacao.CampanhaId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}] Erro ao verificar campanhas pendentes: {ex.Message}");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}