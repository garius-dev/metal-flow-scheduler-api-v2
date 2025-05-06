// --- Configuração do Serilog ---
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
    Log.Information("Provedor de configuração do Google Cloud Secret Manager adicionado para o projeto: {ProjectId}", googleCloudProjectId);
}
else
{
    Log.Warning("Configuração 'GoogleCloud:ProjectId' não encontrada. O provedor do Secret Manager não será adicionado.");
}


// --- Configuração do bind das configs --- 
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
    Log.Fatal(ex, "Falha grave na configuração dos secrets da aplicação.");
    // Considere sair da aplicação aqui se a configuração de secrets for crítica
    Environment.Exit(1);
}
catch (JsonException ex)
{
    Log.Fatal(ex, "Falha ao desserializar a configuração JSON dos secrets.");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocorreu um erro inesperado ao carregar a configuração.");
    Environment.Exit(1);
}


// --- Configuração do PostgreSQL --- 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(currentConnectionString.ConnectionString,
    npgsqlOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// --- Configuração do Identity --- // Adicionado
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configurações de senha (ajuste conforme sua política de segurança)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    // Configurações de Lockout (bloqueio de conta)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configurações de usuário
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true; // Requer email único

    // Configurações de SignIn
    options.SignIn.RequireConfirmedAccount = false; // Defina como true se usar confirmação de email
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

})
.AddEntityFrameworkStores<ApplicationDbContext>() // Configura o Identity para usar EF Core com seu DbContext
.AddDefaultTokenProviders(); // Adiciona provedores de token para reset de senha, etc.

// --- Configuração do AutoMapper --- 
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// --- Injeção de Dependências --- 
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ILineRepository, LineRepository>();
builder.Services.AddScoped<IWorkCenterRepository, WorkCenterRepository>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationTypeRepository, OperationTypeRepository>();
// Adicionar outros repositórios específicos aqui se forem criados

// 4. Registrar Serviços da Camada de Aplicação (Application - C03, RG11)
builder.Services.AddScoped<IOperationTypeService, OperationTypeService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IWorkCenterService, WorkCenterService>();
builder.Services.AddScoped<ILineService, LineService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Configuração do Serilog ---
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();

// --- Configuração de Autenticação JWT ---



var key = Encoding.ASCII.GetBytes(jwtSecretBinder.Secret);

builder.Services.AddAuthentication(options =>
{
    // Define o esquema de autenticação padrão como JWT Bearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Opcional: define o esquema padrão para SignIn/SignOut também
})
.AddJwtBearer(options =>
{
    // ATENÇÃO: RequireHttpsMetadata DEVE ser true em ambientes de produção!
    // Definido como false aqui para facilitar testes locais sem HTTPS.
    options.RequireHttpsMetadata = false;
    // Salva o token no HttpContext para que possa ser acessado posteriormente se necessário
    options.SaveToken = true;
    // Parâmetros para validar o token JWT recebido
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, // Valida a assinatura do token usando a chave secreta
        IssuerSigningKey = new SymmetricSecurityKey(key), // A chave secreta usada para validar
        ValidateIssuer = true, // Valida se o emissor do token é o esperado
        ValidIssuer = jwtSecretBinder.Issuer, // O emissor esperado (definido em appsettings ou user-secrets)
        ValidateAudience = true, // Valida se o público do token é o esperado
        ValidAudience = jwtSecretBinder.Audience, // O público esperado (definido em appsettings ou user-secrets)
        ValidateLifetime = true, // Valida se o token não expirou
        ClockSkew = TimeSpan.Zero // Define a tolerância de tempo para expiração (zero é mais rigoroso)
    };
    // Opcional: Configurar eventos para lidar com falhas de autenticação JWT ou token validado
    // options.Events = new JwtBearerEvents
    // {
    //     OnAuthenticationFailed = context => {
    //         Log.Error(context.Exception, "Falha na autenticação JWT");
    //         return Task.CompletedTask;
    //     },
    //     OnTokenValidated = context => {
    //         Log.Information("Token JWT validado com sucesso para o usuário: {UserName}", context.Principal?.Identity?.Name);
    //         return Task.CompletedTask;
    //     }
    // };
});

// --- Configuração de Autorização ---
// Adiciona os serviços de autorização
builder.Services.AddAuthorization(options => AuthorizationPolicies.ConfigurePolicies(options));

// --- Configuraçãodo Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configuração para a versão 1
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MetalFlowScheduler.Api - V1",
        Version = "v1"
    });

    // Configuração para o Swagger UI entender e permitir o envio do token JWT (Bearer)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization", // Nome do cabeçalho HTTP
        Type = SecuritySchemeType.ApiKey, // Tipo de esquema (ApiKey é usado para cabeçalhos)
        Scheme = "Bearer", // O esquema de autenticação (Bearer)
        BearerFormat = "JWT", // Formato do token (JWT)
        In = ParameterLocation.Header, // Onde o token será enviado (no cabeçalho)
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer ' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });

    // Adiciona a exigência de segurança (o token Bearer) para todos os endpoints no Swagger UI
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
            new string[] {} // Escopos necessários (vazio para JWT simples)
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

// --- Configuração do Versionamento do Swagger ---
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Lê a versão de um segmento da URL, ex.: /api/v2
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Formato do grupo (ex.: v2)
    options.SubstituteApiVersionInUrl = true;
});

// ---  Configuração CORS para permitir requisições do frontend React ---
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



// --- Configuração do Serilog ---
app.UseSerilogRequestLogging();

// --- Configuração do Versionamento do Swagger ---
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

// ---  Configuração CORS para permitir requisições do frontend React ---
app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Iniciando a aplicação MetalFlowScheduler API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
