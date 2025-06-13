using System;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class InvestimentoService
{
    private readonly SupabaseService _supabaseService;
    private readonly ILogger<InvestimentoService> _logger;

    public InvestimentoService(SupabaseService supabaseService, ILogger<InvestimentoService> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task<List<Investimento>> GetActiveInvestments()
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Investimento>()
            .Filter("status", Operator.Equals, "Ativo")
            .Get();
        return response.Models;
    }

    public async Task<Investimento> GetInvestmentById(long id)
    {
        var client = _supabaseService.GetClient();
        var response = await client.From<Investimento>()
            .Filter("id", Operator.Equals, id.ToString()) // Convertendo id para string
            .Get();
        var investimento = response.Models.FirstOrDefault();
        return investimento ?? new Investimento { Id = id };
    }

    public async Task UpdateInvestmentStatus(long investimentoId)
    {
        var client = _supabaseService.GetClient();
        var investment = await GetInvestmentById(investimentoId);
        if (investment != null && investment.TotalInvestido >= investment.Meta)
        {
            await client.From<Investimento>()
                .Where(i => i.Id == investimentoId)
                .Set(i => i.Status, "Completado")
                .Update();
        }
    }
}