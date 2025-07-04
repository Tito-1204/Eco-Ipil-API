using System.Text;
using EcoIpil.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using EcoIpil.API.DTOs;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "EcoIpil API", 
        Version = "v1",
        Description = "API do sistema EcoIpil para gestão de usuários e recursos"
    });

    // Configurar exemplos de requisição
    c.MapType<BaseRequestDTO>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            {"token", new OpenApiSchema 
            { 
                Type = "string",
                Description = "Token JWT para autenticação",
                Example = new Microsoft.OpenApi.Any.OpenApiString("seu_token_jwt_aqui")
            }}
        }
    });

    // Configurar o Swagger para lidar com upload de arquivos
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Configuração do JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada"))),
            RequireExpirationTime = false,
            RequireSignedTokens = true
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Falha na autenticação: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validado com sucesso");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Desafio de autenticação: {context.Error ?? "Sem erro específico"}");
                return Task.CompletedTask;
            }
        };
    });

// Registro de serviços
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CarteiraService>();
builder.Services.AddScoped<EcopontoService>();
builder.Services.AddScoped<HistoricoService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<ReciclagemService>();
builder.Services.AddScoped<RecompensaService>();
builder.Services.AddScoped<CampanhaService>();
builder.Services.AddScoped<InvestimentoService>();
builder.Services.AddScoped<InvestirService>();
builder.Services.AddScoped<ConquistasService>();
builder.Services.AddScoped<AtividadeService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<NotificacaoService>();
builder.Services.AddScoped<ConfiguracaoService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddHostedService<TicketExpirationService>();
builder.Services.AddHostedService<NotificacaoSchedulerService>();
builder.Services.AddHostedService<InvestimentoNotificacaoSchedulerService>();
builder.Services.AddHostedService<EcopontoNotificacaoSchedulerService>(); 
builder.Services.AddHostedService<RecompensaNotificacaoSchedulerService>(); 
builder.Services.AddHostedService<CampanhaVerificationService>();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddHttpContextAccessor();

// Adicionar suporte a sessões
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Configuração da porta para o Railway
builder.WebHost.UseUrls($"http://0.0.0.0:3000");

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoIpil API v1");
        c.DocExpansion(DocExpansion.None);
    });
}

if (!app.Environment.IsDevelopment()) 
{
    app.UseHttpsRedirection();
}

// Adicionar o middleware de sessão antes de outros middlewares que dependem de HttpContext
app.UseSession();

app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();