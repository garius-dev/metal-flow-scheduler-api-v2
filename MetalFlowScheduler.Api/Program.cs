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

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    //.WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// --- Configura��o do PostgreSQL --- 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreDevelopConnection"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));

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

// --- Configura��o do Serilog ---
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();

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
