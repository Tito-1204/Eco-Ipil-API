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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EcoIpil.API.Services;

public class TicketService
{
    private readonly Supabase.Client _supabaseClient; // Cliente para operações gerais
    private readonly Supabase.Client _supabaseAdminClient; // Cliente para operações administrativas (Storage)
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
        _supabaseAdminClient = supabaseService.GetAdminClient(); // Cliente admin para Storage
        _usuarioService = usuarioService;
        _logger = logger;
        _authService = authService;
    }

    // Método auxiliar para gerar um ticket_code de 12 caracteres
    private string GenerateTicketCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Criar um novo ticket
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
                return (false, "Tipo de operação inválido. Use 'LevantamentoExpress' ou 'PagamentoMao'.", null);
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
                return (false, "Carteira digital não encontrada para o usuário.", null);
            }

            if (carteira.Saldo < ticketDTO.Valor)
            {
                return (false, $"Saldo insuficiente. Saldo atual: {carteira.Saldo}, Valor solicitado: {ticketDTO.Valor}.", null);
            }

            carteira.Saldo -= ticketDTO.Valor;
            await _supabaseClient
                .From<CarteiraDigital>()
                .Where(c => c.UsuarioId == userId)
                .Update(carteira);

            var ticket = new Ticket
            {
                Id = DateTime.UtcNow.Ticks,
                CreatedAt = DateTime.UtcNow,
                TipoOperacao = ticketDTO.TipoOperacao,
                Descricao = ticketDTO.Descricao,
                Status = ticketDTO.TipoOperacao == "LevantamentoExpress" ? "Invalidado" : "Valido",
                DataValidade = DateTime.UtcNow.AddDays(7),
                Saldo = ticketDTO.Valor,
                TicketCode = GenerateTicketCode(),
                UsuarioId = userId
            };

            await _supabaseClient.From<Ticket>().Insert(ticket);

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

            _logger.LogInformation("Ticket criado com sucesso para usuário {UserId}: {TicketId}", userId, ticket.Id);
            return (true, "Ticket criado com sucesso.", ticketResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ticket para usuário.");
            return (false, $"Erro ao criar ticket: {ex.Message}", null);
        }
    }

    // Listar tickets do usuário
    public async Task<(bool success, string message, TicketListResponseDTO? tickets)> ListarTickets(string token, string? status, int? pagina, int? limite)
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
                .From<Ticket>()
                .Where(t => t.UsuarioId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Filter("status", Operator.Equals, status);
            }

            var totalResponse = await query.Get();
            int total = totalResponse.Models.Count;

            if (pagina.HasValue && limite.HasValue)
            {
                int from = (pagina.Value - 1) * limite.Value;
                int to = from + limite.Value - 1;
                query = query.Range(from, to);
            }

            var response = await query.Get();
            var tickets = response.Models.Select(t => new TicketResponseDTO
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                TipoOperacao = t.TipoOperacao,
                Descricao = t.Descricao,
                Status = t.Status,
                DataValidade = t.DataValidade,
                Saldo = t.Saldo,
                TicketCode = t.TicketCode
            }).ToList();

            var result = new TicketListResponseDTO
            {
                Tickets = tickets,
                Meta = new PaginationMeta
                {
                    Total = total,
                    Pagina = pagina ?? 1,
                    Limite = limite ?? total,
                    Paginas = limite.HasValue ? (int)Math.Ceiling((double)total / limite.Value) : 1
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

    // Obter detalhes de um ticket específico
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

    /// <summary>
    /// Método auxiliar para fazer upload do PDF para o bucket do Supabase e retornar o link público
    /// </summary>
    private async Task<(bool Success, string Message, string? PublicUrl)> UploadPdfToBucket(byte[] pdfBytes, long ticketId, long userId)
    {
        try
        {
            // Nome do arquivo no bucket: ticket-<ticketId>-user-<userId>.pdf
            var fileName = $"ticket-{ticketId}-user-{userId}.pdf";

            // Fazer upload do arquivo para o bucket "tickets-pdfs" usando o cliente admin
            using var memoryStream = new MemoryStream(pdfBytes);
            var response = await _supabaseAdminClient.Storage
                .From("tickets-pdfs")
                .Upload(pdfBytes, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = "application/pdf",
                    CacheControl = "3600",
                    Upsert = true // Sobrescrever se já existir
                });

            // Verificar se o upload foi bem-sucedido
            if (response == null)
            {
                return (false, "Erro ao fazer upload do PDF para o bucket.", null);
            }

            // Gerar o link público para o arquivo usando o cliente admin
            var publicUrl = _supabaseAdminClient.Storage
                .From("tickets-pdfs")
                .GetPublicUrl(fileName);

            return (true, "PDF carregado com sucesso para o bucket.", publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao fazer upload do PDF para o bucket: {ex.Message}");
            return (false, $"Erro ao fazer upload do PDF: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Gera um PDF para um ticket de pagamento a mão e o armazena no bucket do Supabase
    /// </summary>
    public async Task<(bool Success, string Message, byte[]? PdfBytes, string? DownloadUrl)> GerarPdfTicket(string token, long ticketId)
    {
        try
        {
            var usuarioId = _authService.ObterIdDoToken(token);
            
            if (usuarioId == null)
            {
                return (false, "Token inválido ou expirado", null, null);
            }
            
            var ticket = await _supabaseClient.From<Ticket>()
                .Select("*")
                .Filter("id", Operator.Equals, ticketId)
                .Filter("usuario_id", Operator.Equals, usuarioId.Value)
                .Single();
            
            if (ticket == null)
            {
                return (false, "Ticket não encontrado ou não pertence ao usuário", null, null);
            }
            
            if (ticket.Status != "Valido")
            {
                return (false, $"Ticket com status {ticket.Status} não pode ser exportado", null, null);
            }
            
            if (ticket.TipoOperacao != "PagamentoMao")
            {
                return (false, "Apenas tickets de pagamento a mão podem ser exportados como PDF", null, null);
            }
            
            var usuario = await _usuarioService.ObterUsuarioPorId(usuarioId.Value);
            if (usuario == null)
            {
                return (false, "Dados do usuário não encontrados", null, null);
            }
            
            // Gerar o PDF
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, ticket, usuario));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();

            // Fazer upload do PDF para o bucket do Supabase
            var uploadResult = await UploadPdfToBucket(pdfBytes, ticketId, usuarioId.Value);
            if (!uploadResult.Success)
            {
                return (false, uploadResult.Message, null, null);
            }

            return (true, "PDF gerado e armazenado com sucesso", pdfBytes, uploadResult.PublicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao gerar PDF para o ticket {ticketId}");
            return (false, $"Erro ao gerar PDF: {ex.Message}", null, null);
        }
    }
    
    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.ConstantItem(120).Height(60).Placeholder();
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter().Text("EcoIpil").Bold().FontSize(20);
                col.Item().AlignCenter().Text("Comprovante de Ticket").FontSize(14);
            });
            row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"));
        });
    }
    
    private void ComposeContent(IContainer container, Ticket ticket, Usuario usuario)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Item().PaddingVertical(5).Component(new TicketInfoSection("Informações do Usuário"));
            col.Item().Border(1).Background(Colors.Grey.Lighten3).Padding(10).Column(c =>
            {
                c.Item().Text($"Nome: {usuario.Nome}");
                c.Item().Text($"Email: {usuario.Email}");
                c.Item().Text($"ID: {usuario.Id}");
            });
            
            col.Item().PaddingTop(15).PaddingVertical(5).Component(new TicketInfoSection("Detalhes do Ticket"));
            col.Item().Border(1).Background(Colors.Grey.Lighten3).Padding(10).Column(c =>
            {
                c.Item().Text(text => text.Span($"Código: {ticket.TicketCode}").Bold());
                c.Item().Text($"Tipo: {ticket.TipoOperacao}");
                c.Item().Text($"Saldo: {ticket.Saldo:C2}");
                c.Item().Text($"Data de Emissão: {ticket.CreatedAt:dd/MM/yyyy HH:mm}");
                c.Item().Text($"Validade: {ticket.DataValidade:dd/MM/yyyy HH:mm}");
                c.Item().Text($"Status: {ticket.Status}");
                
                if (!string.IsNullOrEmpty(ticket.Descricao))
                {
                    c.Item().Text($"Descrição: {ticket.Descricao}");
                }
            });
            
            col.Item().PaddingTop(15).PaddingVertical(5).Component(new TicketInfoSection("Instruções"));
            col.Item().Border(1).Background(Colors.Grey.Lighten3).Padding(10).Column(c =>
            {
                c.Item().Text("1. Apresente este ticket a um agente EcoIpil.");
                c.Item().Text("2. O agente irá inserir o código manualmente.");
                c.Item().Text("3. Aguarde a confirmação do pagamento.");
                c.Item().Text("4. Mantenha este comprovante até o processamento completo.");
                c.Item().Text("5. Este ticket é pessoal e intransferível.");
                c.Item().Text(text => text.Span($"6. Código de verificação: {ticket.TicketCode}").Bold());
            });
            
            col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(c =>
            {
                c.Item().Text(text => text.Span("Avisos Importantes:").Bold());
                c.Item().Text("• Este documento é um comprovante oficial do EcoIpil.");
                c.Item().Text("• O ticket tem validade conforme data indicada acima.");
                c.Item().Text("• Em caso de dúvidas, entre em contato com nossa central de suporte.");
            });
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(text => 
            {
                text.Span("EcoIpil © ").Bold();
                text.Span($"{DateTime.Now.Year} - Todos os direitos reservados").FontSize(10);
            });
            
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Documento gerado em: ").FontSize(10);
                text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(10);
            });
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
        container.BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(5).Text(Title).Bold().FontSize(14);
    }
}