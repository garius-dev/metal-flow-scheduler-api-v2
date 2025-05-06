// --- Configura��o do Serilog ---
using Serilog.Events;
using Serilog;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.EntityFrameworkCore;
using System;
using MetalFlowScheduler.Api.Infrastructure.Data;
using MetalFlowScheduler.Api.Application.Mappers;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Infrastructure.Data.Repositories;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Application.Services;
using MetalFlowScheduler.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Google.Cloud.SecretManager.V1;
using Gcp.SecretManager.Provider;
using MetalFlowScheduler.Api.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MetalFlowScheduler.Api.Helpers;
using MetalFlowScheduler.Api.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    //.WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);


var googleCloudProjectId = builder.Configuration["GoogleCloud:ProjectId"];

if (!string.IsNullOrEmpty(googleCloudProjectId))
{
    builder.Configuration.AddGcpSecretManager(
        options =>
        {
            options.ProjectId = googleCloudProjectId;
        });
    Log.Information("Provedor de configura��o do Google Cloud Secret Manager adicionado para o projeto: {ProjectId}", googleCloudProjectId);
}
else
{
    Log.Warning("Configura��o 'GoogleCloud:ProjectId' n�o encontrada. O provedor do Secret Manager n�o ser� adicionado.");
}


// --- Configura��o do bind das configs --- 
JwtSecretConfig jwtSecretBinder = new JwtSecretConfig();
ConnectionStringConfig currentConnectionString = new ConnectionStringConfig();
try
{
    jwtSecretBinder = LoadConfigHelper.LoadConfigFromSecret<JwtSecretConfig>(builder.Configuration, "MetalFlowScheduler-JwtSettings");
    builder.Services.AddSingleton(Options.Create(jwtSecretBinder));

    ConnectionStringsConfig allConnectionStringConfigBinder = LoadConfigHelper.LoadConfigFromSecret<ConnectionStringsConfig>(builder.Configuration, "MetalFlowScheduler-ConnectionStrings");
    currentConnectionString = LoadConfigHelper.GetConnectionStringForEnvironment(allConnectionStringConfigBinder, builder.Environment);
    builder.Services.AddSingleton(Options.Create(currentConnectionString));
}
catch (InvalidOperationException ex)
{
    Log.Fatal(ex, "Falha grave na configura��o dos secrets da aplica��o.");
    // Considere sair da aplica��o aqui se a configura��o de secrets for cr�tica
    Environment.Exit(1);
}
catch (JsonException ex)
{
    Log.Fatal(ex, "Falha ao desserializar a configura��o JSON dos secrets.");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocorreu um erro inesperado ao carregar a configura��o.");
    Environment.Exit(1);
}


// --- Configura��o do PostgreSQL --- 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(currentConnectionString.ConnectionString,
    npgsqlOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// --- Configura��o do Identity --- // Adicionado
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configura��es de senha (ajuste conforme sua pol�tica de seguran�a)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    // Configura��es de Lockout (bloqueio de conta)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configura��es de usu�rio
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true; // Requer email �nico

    // Configura��es de SignIn
    options.SignIn.RequireConfirmedAccount = false; // Defina como true se usar confirma��o de email
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

})
.AddEntityFrameworkStores<ApplicationDbContext>() // Configura o Identity para usar EF Core com seu DbContext
.AddDefaultTokenProviders(); // Adiciona provedores de token para reset de senha, etc.

// --- Configura��o do AutoMapper --- 
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// --- Inje��o de Depend�ncias --- 
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ILineRepository, LineRepository>();
builder.Services.AddScoped<IWorkCenterRepository, WorkCenterRepository>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationTypeRepository, OperationTypeRepository>();
// Adicionar outros reposit�rios espec�ficos aqui se forem criados

// 4. Registrar Servi�os da Camada de Aplica��o (Application - C03, RG11)
builder.Services.AddScoped<IOperationTypeService, OperationTypeService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IWorkCenterService, WorkCenterService>();
builder.Services.AddScoped<ILineService, LineService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Configura��o do Serilog ---
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();

// --- Configura��o de Autentica��o JWT ---



var key = Encoding.ASCII.GetBytes(jwtSecretBinder.Secret);

builder.Services.AddAuthentication(options =>
{
    // Define o esquema de autentica��o padr�o como JWT Bearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Opcional: define o esquema padr�o para SignIn/SignOut tamb�m
})
.AddJwtBearer(options =>
{
    // ATEN��O: RequireHttpsMetadata DEVE ser true em ambientes de produ��o!
    // Definido como false aqui para facilitar testes locais sem HTTPS.
    options.RequireHttpsMetadata = false;
    // Salva o token no HttpContext para que possa ser acessado posteriormente se necess�rio
    options.SaveToken = true;
    // Par�metros para validar o token JWT recebido
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, // Valida a assinatura do token usando a chave secreta
        IssuerSigningKey = new SymmetricSecurityKey(key), // A chave secreta usada para validar
        ValidateIssuer = true, // Valida se o emissor do token � o esperado
        ValidIssuer = jwtSecretBinder.Issuer, // O emissor esperado (definido em appsettings ou user-secrets)
        ValidateAudience = true, // Valida se o p�blico do token � o esperado
        ValidAudience = jwtSecretBinder.Audience, // O p�blico esperado (definido em appsettings ou user-secrets)
        ValidateLifetime = true, // Valida se o token n�o expirou
        ClockSkew = TimeSpan.Zero // Define a toler�ncia de tempo para expira��o (zero � mais rigoroso)
    };
    // Opcional: Configurar eventos para lidar com falhas de autentica��o JWT ou token validado
    // options.Events = new JwtBearerEvents
    // {
    //     OnAuthenticationFailed = context => {
    //         Log.Error(context.Exception, "Falha na autentica��o JWT");
    //         return Task.CompletedTask;
    //     },
    //     OnTokenValidated = context => {
    //         Log.Information("Token JWT validado com sucesso para o usu�rio: {UserName}", context.Principal?.Identity?.Name);
    //         return Task.CompletedTask;
    //     }
    // };
});

// --- Configura��o de Autoriza��o ---
// Adiciona os servi�os de autoriza��o
builder.Services.AddAuthorization(options => AuthorizationPolicies.ConfigurePolicies(options));

// --- Configura��odo Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configura��o para a vers�o 1
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MetalFlowScheduler.Api - V1",
        Version = "v1"
    });

    // Configura��o para o Swagger UI entender e permitir o envio do token JWT (Bearer)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization", // Nome do cabe�alho HTTP
        Type = SecuritySchemeType.ApiKey, // Tipo de esquema (ApiKey � usado para cabe�alhos)
        Scheme = "Bearer", // O esquema de autentica��o (Bearer)
        BearerFormat = "JWT", // Formato do token (JWT)
        In = ParameterLocation.Header, // Onde o token ser� enviado (no cabe�alho)
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer ' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });

    // Adiciona a exig�ncia de seguran�a (o token Bearer) para todos os endpoints no Swagger UI
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // Deve corresponder ao nome definido em AddSecurityDefinition
                }
            },
            new string[] {} // Escopos necess�rios (vazio para JWT simples)
        }
    });

    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;

        var versions = apiDesc.ActionDescriptor?.EndpointMetadata
            .OfType<ApiVersionAttribute>()
            .SelectMany(attr => attr.Versions)
            .Select(v => $"v{v.MajorVersion}") ?? Enumerable.Empty<string>();

        return versions.Contains(docName);
    });
});

// --- Configura��o do Versionamento do Swagger ---
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // L� a vers�o de um segmento da URL, ex.: /api/v2
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Formato do grupo (ex.: v2)
    options.SubstituteApiVersionInUrl = true;
});

// ---  Configura��o CORS para permitir requisi��es do frontend React ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:5173", "https://localhost:5173", "https://jackal-infinite-penguin.ngrok-free.app")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


var app = builder.Build();



// --- Configura��o do Serilog ---
app.UseSerilogRequestLogging();

// --- Configura��o do Versionamento do Swagger ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MetalFlowScheduler API V1");
        options.RoutePrefix = "swagger";
        options.DefaultModelExpandDepth(-1); // Opcional: oculta o schema de modelos
    });

}

// ---  Configura��o CORS para permitir requisi��es do frontend React ---
app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Iniciando a aplica��o MetalFlowScheduler API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplica��o falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
