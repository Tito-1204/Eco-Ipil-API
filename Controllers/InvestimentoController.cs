using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.DTOs;
using EcoIpil.API.Services;
using EcoIpil.API.Models; // Adicionado para o modelo Investir
using System.Threading.Tasks;
using System.Linq;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/investimento")]
public class InvestimentoController : ControllerBase
{
    private readonly InvestimentoService _investimentoService;
    private readonly InvestirService _investirService;
    private readonly UsuarioService _usuarioService;
    private readonly SupabaseService _supabaseService; // Adicionado
    private readonly ILogger<InvestimentoController> _logger;

    public InvestimentoController(
        InvestimentoService investimentoService,
        InvestirService investirService,
        UsuarioService usuarioService,
        SupabaseService supabaseService, // Adicionado ao construtor
        ILogger<InvestimentoController> logger)
    {
        _investimentoService = investimentoService;
        _investirService = investirService;
        _usuarioService = usuarioService;
        _supabaseService = supabaseService; // Atribuído
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllInvestments()
    {
        try
        {
            var investments = await _investimentoService.GetActiveInvestments();
            var response = investments.Select(i => new InvestimentoResponseDTO
            {
                Id = i.Id,
                Nome = i.Nome,
                TotalInvestido = i.TotalInvestido,
                Tipo = i.Tipo,
                Meta = (long)i.Meta, // Cast explícito de double para long
                Status = i.Status,
                Descricao = i.Descricao
            }).ToList();

            return Ok(new
            {
                status = true,
                message = "Investimentos obtidos com sucesso",
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar investimentos");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvestmentById(long id, [FromQuery] string token)
    {
        try
        {
            // Validar o token
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { status = false, message = "Token de autenticação não fornecido" });
            }

            var (tokenValid, tokenMessage, _) = await _usuarioService.ValidateToken(token);
            if (!tokenValid)
            {
                return Unauthorized(new { status = false, message = tokenMessage });
            }

            // Buscar o investimento pelo ID
            var investment = await _investimentoService.GetInvestmentById(id);
            if (investment == null)
            {
                return NotFound(new { status = false, message = "Investimento não encontrado" });
            }

            // Mapear para o DTO de resposta
            var response = new InvestimentoResponseDTO
            {
                Id = investment.Id,
                Nome = investment.Nome,
                TotalInvestido = investment.TotalInvestido,
                Tipo = investment.Tipo,
                Meta = (long)investment.Meta, // Cast explícito de double para long
                Status = investment.Status,
                Descricao = investment.Descricao
            };

            return Ok(new
            {
                status = true,
                message = "Investimento obtido com sucesso",
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter investimento com ID {InvestmentId}", id);
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }

    [HttpGet("usuario")]
    public async Task<IActionResult> GetUserInvestments([FromQuery] string token)
    {
        try
        {
            // Validar o token e obter o ID do usuário logado
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { status = false, message = "Token de autenticação não fornecido" });
            }

            var (tokenValid, tokenMessage, userId) = await _usuarioService.ValidateToken(token);
            if (!tokenValid)
            {
                return Unauthorized(new { status = false, message = tokenMessage });
            }

            // Buscar os registros de investimentos do usuário na tabela 'investir'
            var client = _supabaseService.GetClient();
            var userInvestments = await client.From<Investir>()
                .Where(i => i.UsuarioId == userId)
                .Get();

            if (!userInvestments.Models.Any())
            {
                return Ok(new
                {
                    status = true,
                    message = "Nenhum investimento encontrado para o usuário",
                    data = new List<object>()
                });
            }

            // Buscar os detalhes de cada investimento associado
            var investmentDetails = new List<UserInvestmentResponseDTO>();
            foreach (var investir in userInvestments.Models)
            {
                var investment = await _investimentoService.GetInvestmentById(investir.InvestimentoId);
                if (investment != null)
                {
                    investmentDetails.Add(new UserInvestmentResponseDTO
                    {
                        Id = investment.Id,
                        Nome = investment.Nome,
                        TotalInvestido = investment.TotalInvestido,
                        Tipo = investment.Tipo,
                        Meta = (long)investment.Meta, // Cast explícito de double para long
                        Status = investment.Status,
                        PontosInvestidos = investir.PontosInvestidos,
                        DataRetorno = investir.DataRetorno,
                        ValorRetorno = (long?)investir.ValorRetorno,
                        Descricao = investment.Descricao
                    });
                }
            }

            return Ok(new
            {
                status = true,
                message = "Investimentos do usuário obtidos com sucesso",
                data = investmentDetails
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar investimentos do usuário");
            return StatusCode(500, new { status = false, message = "Erro interno ao processar a solicitação" });
        }
    }
}