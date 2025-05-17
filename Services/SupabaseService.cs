using Supabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EcoIpil.API.Services
{
    public class SupabaseService
    {
        private readonly Client _supabaseClient; // Cliente com chave anônima para operações gerais
        private readonly Client _supabaseAdminClient; // Cliente com chave de serviço para operações administrativas
        private readonly ILogger<SupabaseService> _logger;
        private readonly IConfiguration _configuration;

        public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            try
            {
                var url = _configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL não configurada");
                var anonKey = _configuration["Supabase:Key"] ?? throw new InvalidOperationException("Supabase Key não configurada");
                var serviceKey = _configuration["Supabase:ServiceKey"] ?? throw new InvalidOperationException("Supabase ServiceKey não configurada");

                _logger.LogInformation($"Inicializando cliente Supabase com URL: {url}");

                // Cliente para operações gerais (usa anon key)
                _supabaseClient = new Client(url, anonKey);
                _supabaseClient.InitializeAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Cliente Supabase (anon) inicializado com sucesso");

                // Cliente para operações administrativas (usa service_role key)
                _supabaseAdminClient = new Client(url, serviceKey);
                _supabaseAdminClient.InitializeAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Cliente Supabase (admin) inicializado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal ao inicializar cliente Supabase");
                throw;
            }
        }

        public Client GetClient() => _supabaseClient;

        public Client GetAdminClient() => _supabaseAdminClient;

        public async Task<bool> TestarConexao()
        {
            try
            {
                var response = await _supabaseClient.From<Models.Usuario>().Select("*").Single();
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com Supabase");
                return false;
            }
        }
    }
}