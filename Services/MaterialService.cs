using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoIpil.API.DTOs;
using EcoIpil.API.Models;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace EcoIpil.API.Services;

public class MaterialService
{
    private readonly Supabase.Client _supabaseClient;

    public MaterialService(SupabaseService supabaseService)
    {
        _supabaseClient = supabaseService.GetClient();
    }

    public async Task<(bool success, string message, List<MaterialResponseDTO>? materiais)> ListarMateriais(string? classe = null)
    {
        try
        {
            // Construir a consulta
            var query = _supabaseClient.From<Material>().Select("*");

            // Aplicar filtro por classe, se fornecido
            if (!string.IsNullOrEmpty(classe))
            {
                query = query.Filter("classe", Operator.Equals, classe);
            }

            // Ordenar por nome
            query = query.Order("nome", Ordering.Ascending);

            // Executar a consulta
            var response = await query.Get();
            var materiais = response.Models;

            if (materiais == null || !materiais.Any())
            {
                return (true, "Nenhum material encontrado", new List<MaterialResponseDTO>());
            }

            // Mapear para DTO
            var materiaisDTO = materiais.Select(m => new MaterialResponseDTO
            {
                Id = m.Id,
                CreatedAt = m.CreatedAt,
                Nome = m.Nome,
                Classe = m.Classe,
                Valor = m.Valor
            }).ToList();

            return (true, "Materiais obtidos com sucesso", materiaisDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar materiais: {ex.Message}");
            return (false, "Erro ao listar materiais", null);
        }
    }

    public async Task<(bool success, string message, MaterialResponseDTO? material)> ObterMaterial(long id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Material>()
                .Where(m => m.Id == id)
                .Single();

            if (response == null)
            {
                return (false, "Material não encontrado", null);
            }

            var materialDTO = new MaterialResponseDTO
            {
                Id = response.Id,
                CreatedAt = response.CreatedAt,
                Nome = response.Nome,
                Classe = response.Classe,
                Valor = response.Valor
            };

            return (true, "Material obtido com sucesso", materialDTO);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter material: {ex.Message}");
            return (false, "Erro ao obter material", null);
        }
    }
} 