using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using STA.Worker;
using STA.Worker.Data;
using STA.Worker.Data.Repositories;
using STA.Worker.Services;
using STA.Worker.Settings;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Sistema STA - Transferencia de Arquivos";
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // CreateDefaultBuilder já carrega appsettings.json e appsettings.{env}.json.
        // Adicionamos aqui apenas para reloadOnChange e para adicionar env vars.
        config.AddEnvironmentVariables(prefix: "STA_");
    })
    .ConfigureServices((context, services) =>
    {
        // Connection string: env var STA_DB_CONN tem precedência sobre appsettings
        var connectionString = Environment.GetEnvironmentVariable("STA_DB_CONN")
            ?? context.Configuration.GetConnectionString("StaDb")
            ?? throw new InvalidOperationException(
                "Connection string não configurada. Defina 'ConnectionStrings:StaDb' em appsettings.json ou a variável de ambiente 'STA_DB_CONN'.");

        // EF Core + PostgreSQL
        services.AddDbContext<StaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.CommandTimeout(120);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            })
        );

        // Settings: seção StaSettings do appsettings.json
        services.Configure<StaSettings>(
            context.Configuration.GetSection("StaSettings"));

        // Repositories
        services.AddScoped<IParametroRepository, ParametroRepository>();
        services.AddScoped<ILogRepository, LogRepository>();

        // Services
        services.AddSingleton<IFileMaskMatcher, FileMaskMatcher>();
        services.AddSingleton<IFileSizeValidator, FileSizeValidator>();
        services.AddSingleton<IFileLockChecker, FileLockChecker>();
        services.AddSingleton<IPathConfigLoader, PathConfigLoader>();
        services.AddScoped<IFileRetentionService, FileRetentionService>();
        services.AddScoped<IFileTransferService, FileTransferService>();
        services.AddSingleton<IFileCompressor>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<StaSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<FileCompressor>>();
            return new FileCompressor(settings.Arquivo7Zip, logger);
        });

        // Worker
        services.AddHostedService<Worker>();
    });

var host = builder.Build();
host.Run();
