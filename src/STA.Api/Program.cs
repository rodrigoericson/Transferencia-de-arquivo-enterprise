using Microsoft.EntityFrameworkCore;
using STA.Core.Data;
using STA.Core.Data.Repositories;
using STA.Core.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "STA_");

var connectionString = Environment.GetEnvironmentVariable("STA_DB_CONN")
    ?? builder.Configuration.GetConnectionString("StaDb")
    ?? throw new InvalidOperationException(
        "Connection string não configurada. Defina 'ConnectionStrings:StaDb' em appsettings.json ou a variável de ambiente 'STA_DB_CONN'.");

builder.Services.AddDbContext<StaDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.CommandTimeout(120);
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    }));

builder.Services.Configure<StaSettings>(builder.Configuration.GetSection("StaSettings"));

builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IEtapaRepository, EtapaRepository>();
builder.Services.AddScoped<ILogArquivoRepository, LogArquivoRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<StaDbContext>("postgres");

var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];
builder.Services.AddCors(o =>
    o.AddPolicy("Default", p =>
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Default");
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
