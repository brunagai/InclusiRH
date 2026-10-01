using System.Text;
using System.Text.Json.Serialization;
using InclusiRH.Api.Data;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Autodeclaracoes;
using InclusiRH.Api.Services.Background;
using InclusiRH.Api.Services.Colaboradores;
using InclusiRH.Api.Services.Denuncias;
using InclusiRH.Api.Services.Evidencias;
using InclusiRH.Api.Services.Notificacoes;
using InclusiRH.Api.Services.Treinamentos;
using InclusiRH.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // O conversor de papel vem antes para aceitar os apelidos RH e Admin.
        options.JsonSerializerOptions.Converters.Add(new PapelColaboradorJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InclusiRH API",
        Version = "v1",
        Description = "Plataforma de Diversidade, Equidade e Inclusão, canal de denúncias e conformidade."
    });
    options.SchemaFilter<EnumSchemaFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token retornado por POST /api/auth/login. Envie apenas o valor, sem o prefixo Bearer."
    });
    options.OperationFilter<AutorizacaoOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi configurada.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(maxRetryCount: 5);
        sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
    });
    options.EnableSensitiveDataLogging(false);
});

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(
        opcoes =>
            !string.IsNullOrWhiteSpace(opcoes.Key)
            && Encoding.UTF8.GetByteCount(opcoes.Key) >= 32
            && !string.IsNullOrWhiteSpace(opcoes.Issuer)
            && !string.IsNullOrWhiteSpace(opcoes.Audience)
            && opcoes.AccessTokenExpireMinutes is >= 1 and <= 60
            && opcoes.RefreshTokenExpireDays is >= 1 and <= 30,
        "Configure Jwt:Key (mínimo de 32 bytes), Jwt:Issuer, Jwt:Audience, "
        + "Jwt:AccessTokenExpireMinutes (1 a 60) e Jwt:RefreshTokenExpireDays (1 a 30).")
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("A seção Jwt não foi configurada.");

// A identidade do request passa a ser o JWT. RoleClaimType = "role" liga a claim
// emitida no login ao [Authorize(Roles)] e às políticas abaixo.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { erro = "Autenticação necessária." });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { erro = "Você não tem permissão para esta operação." });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Qualquer endpoint novo exige login, salvo se estiver marcado com [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(PoliticasAcesso.Rh, policy =>
        policy.RequireRole(
            nameof(PapelColaborador.RecursosHumanos),
            nameof(PapelColaborador.Administrador)));

    options.AddPolicy(PoliticasAcesso.Administrador, policy =>
        policy.RequireRole(nameof(PapelColaborador.Administrador)));

    options.AddPolicy(PoliticasAcesso.ComiteOuRh, policy =>
        policy.RequireRole(
            nameof(PapelColaborador.ComiteDei),
            nameof(PapelColaborador.RecursosHumanos),
            nameof(PapelColaborador.Administrador)));
});

builder.Services.AddOptions<LgpdOptions>()
    .Bind(builder.Configuration.GetSection(LgpdOptions.SectionName))
    .Validate(
        opcoes => !string.IsNullOrWhiteSpace(opcoes.ChaveAes) && Encoding.UTF8.GetByteCount(opcoes.ChaveAes) == 32,
        "Lgpd:ChaveAes precisa ter exatamente 32 bytes para AES-256.")
    .ValidateOnStart();
builder.Services.AddSingleton<IProtetorDadosSensiveis, ProtetorDadosSensiveisAes>();
builder.Services.AddSingleton<IProtetorConteudoDenuncia, ProtetorConteudoDenuncia>();

builder.Services.AddOptions<EvidenciaOptions>()
    .Bind(builder.Configuration.GetSection(EvidenciaOptions.SectionName))
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Diretorio), "Configure Evidencias:Diretorio.")
    .ValidateOnStart();
builder.Services.AddSingleton<IArmazenamentoEvidencias, ArmazenamentoEvidenciasLocal>();

builder.Services.AddSingleton<IPasswordHasher<Colaborador>, PasswordHasher<Colaborador>>();
builder.Services.AddScoped<IColaboradorService, ColaboradorService>();
builder.Services.AddScoped<IAutodeclaracaoService, AutodeclaracaoService>();
builder.Services.AddScoped<IDenunciaService, DenunciaService>();
builder.Services.AddScoped<ITreinamentoService, TreinamentoService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddOptions<NotificacaoOptions>()
    .Bind(builder.Configuration.GetSection(NotificacaoOptions.SectionName))
    .Validate(
        opcoes => !string.IsNullOrWhiteSpace(opcoes.Remetente)
            && opcoes.DestinatariosDiretoria.Count + opcoes.DestinatariosRhSuperior.Count > 0,
        "Configure Notificacoes:Remetente e ao menos um destinatário da diretoria ou do RH superior.")
    .Validate(
        opcoes => opcoes.Modo != ModoEnvioNotificacao.Smtp
            || (!string.IsNullOrWhiteSpace(opcoes.Smtp.Host) && opcoes.Smtp.Porta is > 0 and <= 65535),
        "No modo Smtp, configure Notificacoes:Smtp:Host e Notificacoes:Smtp:Porta.")
    .Validate(
        opcoes => opcoes.Modo != ModoEnvioNotificacao.Arquivo || !string.IsNullOrWhiteSpace(opcoes.DiretorioArquivos),
        "No modo Arquivo, configure Notificacoes:DiretorioArquivos.")
    .ValidateOnStart();
builder.Services.AddSingleton<INotificadorConformidade, NotificadorConformidade>();

builder.Services.AddOptions<AlertaEscalonamentoOptions>()
    .Bind(builder.Configuration.GetSection(AlertaEscalonamentoOptions.SectionName))
    .Validate(
        opcoes => opcoes.IntervaloMinutos >= 1
            && opcoes.AtrasoInicialSegundos >= 0
            && opcoes.PrazoTratativaDias >= 1
            && opcoes.LoteMaximo is >= 1 and <= 500,
        "Configure BackgroundServices:AlertaEscalonamento com IntervaloMinutos >= 1, "
        + "AtrasoInicialSegundos >= 0, PrazoTratativaDias >= 1 e LoteMaximo entre 1 e 500.")
    .ValidateOnStart();
builder.Services.AddScoped<IProcessadorAlertasDenuncia, ProcessadorAlertasDenuncia>();
builder.Services.AddHostedService<AlertaEscalonamentoWorker>();

builder.Services.AddExceptionHandler<ConcorrenciaExceptionHandler>();
builder.Services.AddExceptionHandler<RegraDeNegocioExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
