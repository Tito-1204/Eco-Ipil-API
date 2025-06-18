using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EcoIpil.API.Models;
using EcoIpil.API.DTOs;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace EcoIpil.API.Services;

public class TicketService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly UsuarioService _usuarioService;
    private readonly ILogger<TicketService> _logger;
    private readonly AuthService _authService;

    public TicketService(
        SupabaseService supabaseService,
        UsuarioService usuarioService,
        ILogger<TicketService> logger,
        AuthService authService)
    {
        _supabaseClient = supabaseService.GetClient();
        _usuarioService = usuarioService;
        _logger = logger;
        _authService = authService;
    }
    
    private string GenerateTicketCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task<(bool success, string message, TicketResponseDTO? ticket)> CriarTicket(TicketCreateDTO ticketDTO)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(ticketDTO.Token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }
            long userId = validationResult.userId;

            if (ticketDTO.TipoOperacao != "LevantamentoExpress" && ticketDTO.TipoOperacao != "PagamentoMao" && ticketDTO.TipoOperacao != "ResgateRecompensa")
            {
                return (false, "Tipo de operação inválido.", null);
            }

            if (ticketDTO.Valor <= 0 && ticketDTO.TipoOperacao != "ResgateRecompensa")
            {
                return (false, "O valor deve ser maior que zero.", null);
            }

            var carteira = await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Single();

            if (carteira == null)
            {
                return (false, "Carteira digital não encontrada.", null);
            }

            if (carteira.Saldo < ticketDTO.Valor)
            {
                return (false, $"Saldo insuficiente. Saldo atual: {carteira.Saldo}, Valor solicitado: {ticketDTO.Valor}.", null);
            }

            carteira.Saldo -= ticketDTO.Valor;
            await _supabaseClient.From<CarteiraDigital>().Update(carteira);

            var ticket = new Ticket
            {
                UsuarioId = userId,
                TicketCode = GenerateTicketCode(),
                TipoOperacao = ticketDTO.TipoOperacao,
                Descricao = ticketDTO.Descricao,
                Saldo = (float)ticketDTO.Valor, // CORREÇÃO: Casting para float
                Status = "Valido",
                CreatedAt = DateTime.UtcNow,
                DataValidade = DateTime.UtcNow.AddDays(7)
            };
            
            if (ticket.TipoOperacao == "LevantamentoExpress") {
                ticket.Status = "Invalidado";
            }

            var insertResponse = await _supabaseClient.From<Ticket>().Insert(ticket);
            var insertedTicket = insertResponse.Models.FirstOrDefault();

            if (insertedTicket == null) {
                return (false, "Falha ao criar o ticket no banco de dados.", null);
            }

            var ticketResponse = new TicketResponseDTO
            {
                Id = insertedTicket.Id,
                CreatedAt = insertedTicket.CreatedAt,
                TipoOperacao = insertedTicket.TipoOperacao,
                Descricao = insertedTicket.Descricao,
                Status = insertedTicket.Status,
                DataValidade = insertedTicket.DataValidade,
                Saldo = insertedTicket.Saldo,
                TicketCode = insertedTicket.TicketCode
            };

            _logger.LogInformation("Ticket criado com sucesso para usuário {UserId}: {TicketId}", userId, insertedTicket.Id);
            return (true, "Ticket criado com sucesso.", ticketResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ticket para usuário.");
            return (false, $"Erro ao criar ticket: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, TicketListResponseDTO? tickets)> ListarTickets(string token, string? status, string? tipoOperacao, int? pagina, int? limite)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }
            long userId = validationResult.userId;

            var query = _supabaseClient
                .From<Ticket>();
                

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Filter("status", Operator.Equals, status);
            }
            
            if (!string.IsNullOrEmpty(tipoOperacao))
            {
                query = query.Filter("tipo_operacao", Operator.Equals, tipoOperacao);
            }

            var countResponse = await query.Count(CountType.Exact);
            _logger.LogInformation($"Found {countResponse} tickets for user {userId}");
            
            int page = pagina ?? 1;
            int pageSize = limite ?? 10;
            int from = (page - 1) * pageSize;
            
            var response = await query
                .Order("created_at", Ordering.Descending)
                .Range(from, from + pageSize - 1)
                .Get();

            var tickets = response.Models;

            var ticketDtos = tickets.Select(t => new TicketResponseDTO
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                TipoOperacao = t.TipoOperacao ?? "",
                Descricao = t.Descricao ?? "",
                Status = t.Status ?? "",
                DataValidade = t.DataValidade,
                Saldo = (float)t.Saldo,
                TicketCode = t.TicketCode ?? ""
            }).ToList();

            var result = new TicketListResponseDTO
            {
                Tickets = ticketDtos,
                Meta = new PaginationMeta
                {
                    Total = countResponse,
                    Pagina = page,
                    Limite = pageSize,
                    Paginas = (int)Math.Ceiling((double)countResponse / pageSize)
                }
            };

            return (true, "Tickets listados com sucesso.", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar tickets para usuário.");
            return (false, $"Erro ao listar tickets: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, TicketResponseDTO? ticket)> ObterTicket(string token, long ticketId)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }
            long userId = validationResult.userId;

            var ticket = await _supabaseClient
                .From<Ticket>()
                .Where(t => t.Id == ticketId && t.UsuarioId == userId)
                .Single();

            if (ticket == null)
            {
                return (false, "Ticket não encontrado ou não pertence ao usuário.", null);
            }

            var ticketResponse = new TicketResponseDTO
            {
                Id = ticket.Id,
                CreatedAt = ticket.CreatedAt,
                TipoOperacao = ticket.TipoOperacao,
                Descricao = ticket.Descricao,
                Status = ticket.Status,
                DataValidade = ticket.DataValidade,
                Saldo = ticket.Saldo,
                TicketCode = ticket.TicketCode
            };

            return (true, "Ticket obtido com sucesso.", ticketResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter ticket.");
            return (false, $"Erro ao obter ticket: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, byte[]? PdfBytes)> GerarPdfTicket(string token, string ticketCode)
    {
        try
        {
            var validationResult = await _usuarioService.ValidateToken(token);
            if (!validationResult.success)
            {
                return (false, validationResult.message, null);
            }
            long userId = validationResult.userId;
            
            var ticket = await _supabaseClient.From<Ticket>()
                .Where(t => t.TicketCode == ticketCode && t.UsuarioId == userId)
                .Single();
            
            if (ticket == null)
            {
                return (false, "Ticket não encontrado ou não pertence ao usuário.", null);
            }
            
            if (ticket.TipoOperacao != "PagamentoMao")
            {
                return (false, "Apenas tickets de 'Pagamento em Mão' podem ser exportados como PDF.", null);
            }

            var usuario = await _usuarioService.ObterUsuarioPorId(userId);
            if (usuario == null)
            {
                return (false, "Dados do usuário não encontrados.", null);
            }
            
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Lato")); 
                    
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, ticket, usuario));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();

            return (true, "PDF gerado com sucesso.", pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao gerar PDF para o ticket com código {ticketCode}");
            return (false, $"Erro ao gerar PDF: {ex.Message}", null);
        }
    }
    
    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter().Text("EcoIpil").Bold().FontSize(24).FontColor(Colors.Green.Darken2);
                col.Item().AlignCenter().Text("Comprovativo de Pedido de Levantamento").FontSize(14).SemiBold();
            });
            row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"));
        });
    }
    
    private void ComposeContent(IContainer container, Ticket ticket, Usuario usuario)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Item().Component(new TicketInfoSection("Informações do Utilizador"));
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
            {
                c.Item().Text(txt => { txt.Span("Nome: ").SemiBold(); txt.Span(usuario.Nome); });
                c.Item().Text(txt => { txt.Span("Email: ").SemiBold(); txt.Span(usuario.Email); });
                c.Item().Text(txt => { txt.Span("ID do Utilizador: ").SemiBold(); txt.Span(usuario.Id.ToString()); });
            });
            
            col.Item().PaddingTop(20).Component(new TicketInfoSection("Detalhes do Ticket"));
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
            {
                c.Item().Text(text => { text.Span("Código do Ticket: ").SemiBold(); text.Span(ticket.TicketCode).Bold().FontSize(14); });
                c.Item().Text(txt => { txt.Span("Tipo de Operação: ").SemiBold(); txt.Span("Pagamento em Mão"); });
                c.Item().Text(txt => { txt.Span("Valor: ").SemiBold(); txt.Span($"{ticket.Saldo:N2} AOA"); });
                c.Item().Text(txt => { txt.Span("Data de Emissão: ").SemiBold(); txt.Span($"{ticket.CreatedAt:dd/MM/yyyy HH:mm}"); });
                c.Item().Text(txt => { txt.Span("Data de Validade: ").SemiBold(); txt.Span($"{ticket.DataValidade:dd/MM/yyyy HH:mm}"); });
                c.Item().Text(txt => { txt.Span("Status: ").SemiBold(); txt.Span(ticket.Status).FontColor(Colors.Green.Darken2).Bold(); });
            });
            
            col.Item().PaddingTop(20).Component(new TicketInfoSection("Instruções"));
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Blue.Lighten5).Padding(10).Column(c =>
            {
                c.Spacing(5);
                c.Item().Text("1. Visite um Ecoponto ou Agente EcoIpil autorizado.");
                c.Item().Text("2. Apresente este documento (impresso ou digital) ao agente.");
                c.Item().Text(text => { text.Span("3. O agente irá validar o "); text.Span("Código do Ticket").Bold(); text.Span(" para processar o seu levantamento."); });
                c.Item().Text("4. Após a validação, o valor será entregue em mãos.");
                c.Item().Text("5. Este ticket é de uso único, pessoal e intransmissível.");
            });
            
            col.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(10).Column(c =>
            {
                c.Item().Text(text => text.Span("Avisos Importantes:").Bold());
                c.Item().Text("• Guarde este documento em segurança até a conclusão da operação.");
                c.Item().Text("• Verifique a data de validade. O ticket não poderá ser usado após expirar.");
                c.Item().Text("• Em caso de dúvidas, contacte o nosso suporte.");
            });
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(10));
            
            text.Span("EcoIpil © ").SemiBold();
            text.Span($"{DateTime.Now.Year} - Agradecemos por ajudar o ambiente! Página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
        });
    }
}

public class TicketInfoSection : IComponent
{
    private string Title { get; }

    public TicketInfoSection(string title)
    {
        Title = title;
    }

    public void Compose(IContainer container)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingBottom(5).Text(Title).Bold().FontSize(16).FontColor(Colors.Green.Darken2);
    }
}