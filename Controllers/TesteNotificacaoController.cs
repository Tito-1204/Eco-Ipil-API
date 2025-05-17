using Microsoft.AspNetCore.Mvc;
using EcoIpil.API.Services;
using System.Threading.Tasks;
using EcoIpil.API.Models;

namespace EcoIpil.API.Controllers;

[ApiController]
[Route("api/v1/teste-notificacao")]
public class TesteNotificacaoController : ControllerBase
{
    private readonly CampanhaService _campanhaService;
    private readonly InvestimentoService _investimentoService;
    private readonly EcopontoService _ecopontoService;
    private readonly RecompensaService _recompensaService;
    private readonly UsuarioService _usuarioService;
    private readonly NotificacaoService _notificacaoService;
    private readonly SupabaseService _supabaseService;

    public TesteNotificacaoController(
        CampanhaService campanhaService,
        InvestimentoService investimentoService,
        EcopontoService ecopontoService,
        RecompensaService recompensaService,
        UsuarioService usuarioService,
        NotificacaoService notificacaoService,
        SupabaseService supabaseService)
    {
        _campanhaService = campanhaService;
        _investimentoService = investimentoService;
        _ecopontoService = ecopontoService;
        _recompensaService = recompensaService;
        _usuarioService = usuarioService;
        _notificacaoService = notificacaoService;
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Testa a criação de notificações para campanhas que iniciam hoje.
    /// </summary>
    [HttpPost("campanhas/iniciando")]
    public async Task<IActionResult> TestarNotificacoesIniciandoCampanhas()
    {
        var (success, message) = await _campanhaService.CriarNotificacoesIniciandoCampanhas();
        if (success)
        {
            return Ok(new { status = true, message });
        }
        return BadRequest(new { status = false, message });
    }

    /// <summary>
    /// Testa a criação de notificações para campanhas que estão prestes a encerrar.
    /// </summary>
    [HttpPost("campanhas/encerrando")]
    public async Task<IActionResult> TestarNotificacoesCampanhasEncerrando([FromQuery] int diasRestantes = 3)
    {
        var (success, message) = await _campanhaService.CriarNotificacoesCampanhasEncerrando(diasRestantes);
        if (success)
        {
            return Ok(new { status = true, message });
        }
        return BadRequest(new { status = false, message });
    }

    /// <summary>
    /// Testa a criação de notificações gerais para novos investimentos.
    /// </summary>
    [HttpPost("investimentos/novos")]
    public async Task<IActionResult> TestarNotificacoesNovosInvestimentos()
    {
        try
        {
            var investimentosAtivos = await _investimentoService.GetActiveInvestments();
            var agora = DateTime.UtcNow;
            var novosInvestimentosNotificados = 0;

            foreach (var investimento in investimentosAtivos)
            {
                var semanaCriacao = (agora - investimento.CreatedAt).TotalDays <= 7;

                if (semanaCriacao)
                {
                    var mensagemGeral = $"Novo investimento disponível: {investimento.Nome}! Invista seus pontos para lucrar em 6 meses ou 1 ano.";
                    var (successGeral, messageGeral) = await _notificacaoService.CriarNotificacaoGeral(
                        mensagem: mensagemGeral,
                        tipo: "Novo Investimento",
                        dataExpiracao: null
                    );

                    if (successGeral)
                    {
                        novosInvestimentosNotificados++;
                    }
                }
            }

            if (novosInvestimentosNotificados > 0)
            {
                return Ok(new { status = true, message = $"{novosInvestimentosNotificados} notificações gerais criadas para novos investimentos." });
            }
            return Ok(new { status = true, message = "Nenhum novo investimento encontrado na última semana." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao criar notificações de novos investimentos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Testa a criação de notificações pessoais de convite para investimentos ativos.
    /// </summary>
    [HttpPost("investimentos/convites")]
    public async Task<IActionResult> TestarNotificacoesConvitesInvestimentos()
    {
        try
        {
            var investimentosAtivos = await _investimentoService.GetActiveInvestments();
            var usuarios = await _supabaseService.GetClient().From<Usuario>().Select("*").Get();
            var agora = DateTime.UtcNow;
            var notificacoesCriadas = 0;

            foreach (var investimento in investimentosAtivos)
            {
                var semanaCriacao = (agora - investimento.CreatedAt).TotalDays <= 7;

                foreach (var usuario in usuarios.Models)
                {
                    var (successCarteira, _, pontos) = await _usuarioService.GetUserWallet(usuario.Id);
                    if (!successCarteira || pontos < 2000) continue;

                    var jaInvestiu = (await _supabaseService.GetClient().From<Investir>()
                        .Where(i => i.UsuarioId == usuario.Id && i.InvestimentoId == investimento.Id)
                        .Get()).Models.Any();

                    if (jaInvestiu || investimento.TotalInvestido >= investimento.Meta) continue;

                    var mensagem = semanaCriacao
                        ? $"Novo investimento '{investimento.Nome}' disponível! Invista seus {pontos} pontos para lucrar até 55% em 6 meses ou 1 ano."
                        : $"Invista em '{investimento.Nome}'! Seus {pontos} pontos podem render lucros em 6 meses ou 1 ano.";
                    var (success, message) = await _notificacaoService.CriarNotificacaoPessoal(
                        usuarioId: usuario.Id,
                        mensagem: mensagem,
                        tipo: "Convite Investimento",
                        dataExpiracao: DateTime.UtcNow.AddDays(7)
                    );

                    if (success)
                    {
                        notificacoesCriadas++;
                    }
                }
            }

            return Ok(new { status = true, message = $"{notificacoesCriadas} notificações pessoais de convite criadas para investimentos." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao criar notificações de convites de investimentos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Testa a criação de notificações gerais para novos ecopontos (criados nos últimos 5 dias).
    /// </summary>
    [HttpPost("ecopontos/novos")]
    public async Task<IActionResult> TestarNotificacoesNovosEcopontos()
    {
        try
        {
            var agora = DateTime.UtcNow;
            var ecopontos = (await _ecopontoService.ListarEcopontos()).ecopontos;
            var novosEcopontos = ecopontos?.Where(e => (agora - e.CreatedAt).TotalDays <= 5).ToList();
            var notificacoesCriadas = 0;

            if (novosEcopontos != null && novosEcopontos.Any())
            {
                foreach (var ecoponto in novosEcopontos)
                {
                    var mensagem = $"Novo ecoponto disponível: {ecoponto.Nome}! Localização: {ecoponto.Localizacao}. Recicle e contribua para um futuro sustentável!";
                    var (success, message) = await _notificacaoService.CriarNotificacaoGeral(
                        mensagem: mensagem,
                        tipo: "Novo Ecoponto",
                        dataExpiracao: null
                    );

                    if (success)
                    {
                        notificacoesCriadas++;
                    }
                }
            }

            if (notificacoesCriadas > 0)
            {
                return Ok(new { status = true, message = $"{notificacoesCriadas} notificações gerais criadas para novos ecopontos." });
            }
            return Ok(new { status = true, message = "Nenhum novo ecoponto encontrado nos últimos 5 dias." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao criar notificações de novos ecopontos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Testa a criação de notificações pessoais para novas recompensas (criadas nas últimas 24 horas).
    /// </summary>
    [HttpPost("recompensas/novas")]
    public async Task<IActionResult> TestarNotificacoesNovasRecompensas()
    {
        try
        {
            var agora = DateTime.UtcNow;
            var recompensas = (await _recompensaService.ListarRecompensasSemAutenticacao()).recompensas;
            var novasRecompensas = recompensas?.Where(r => (agora - r.CreatedAt).TotalHours <= 24).ToList();
            var notificacoesCriadas = 0;

            if (novasRecompensas != null && novasRecompensas.Any())
            {
                var usuarios = await _supabaseService.GetClient().From<Usuario>().Select("*").Get();

                foreach (var recompensa in novasRecompensas)
                {
                    foreach (var usuario in usuarios.Models)
                    {
                        var jaResgatou = (await _supabaseService.GetClient().From<RecompensaUsuario>()
                            .Where(ru => ru.UsuarioId == usuario.Id && ru.RecompensaId == recompensa.Id)
                            .Get()).Models.Any();

                        if (jaResgatou || recompensa.QtRestante <= 0) continue;

                        var mensagem = $"🎉 Nova recompensa disponível: {recompensa.Nome}! Custa apenas {recompensa.Pontos} pontos. Resgate agora e aproveite essa oferta incrível!";
                        var (success, message) = await _notificacaoService.CriarNotificacaoPessoal(
                            usuarioId: usuario.Id,
                            mensagem: mensagem,
                            tipo: "Nova Recompensa",
                            dataExpiracao: DateTime.UtcNow.AddDays(7)
                        );

                        if (success)
                        {
                            notificacoesCriadas++;
                        }
                    }
                }
            }

            if (notificacoesCriadas > 0)
            {
                return Ok(new { status = true, message = $"{notificacoesCriadas} notificações pessoais criadas para novas recompensas." });
            }
            return Ok(new { status = true, message = "Nenhuma nova recompensa encontrada nas últimas 24 horas." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = false, message = $"Erro ao criar notificações de novas recompensas: {ex.Message}" });
        }
    }

    [HttpGet("test-email")]
    public async Task<IActionResult> TestEmail()
    {
        try
        {
            await _notificacaoService.EnviarEmailNotificacao("tapanacara124@gmail.com", "Teste de email", "Teste");
            return Ok("Email de teste enviado");
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao enviar email: {ex.Message}");
        }
    }
}