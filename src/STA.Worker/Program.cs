using Microsoft.Extensions.Options;
using STA.Worker;
using STA.Core.DependencyInjection;
using STA.Core.Services;
using STA.Core.Services.Transports;
using STA.Core.Settings;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Sistema STA - Transferencia de Arquivos";
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddEnvironmentVariables(prefix: "STA_");
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = Environment.GetEnvironmentVariable("STA_DB_CONN")
            ?? context.Configuration.GetConnectionString("StaDb")
            ?? throw new InvalidOperationException(
                "Connection string não configurada. Defina 'ConnectionStrings:StaDb' em appsettings.json ou a variável de ambiente 'STA_DB_CONN'.");

        // Shared infrastructure
        services.AddStaDatabase(connectionString, migrationsAssembly: "STA.Worker");
        services.Configure<StaSettings>(context.Configuration.GetSection("StaSettings"));
        services.AddStaRepositories();
        services.AddStaSftpTransports();

        // File transfer services
        services.AddSingleton<IFileMaskMatcher, FileMaskMatcher>();
        services.AddSingleton<IFileSizeValidator, FileSizeValidator>();
        services.AddSingleton<IFileLockChecker, FileLockChecker>();
        services.AddSingleton<IFilePurgeService, FilePurgeService>();
        services.AddSingleton<IPathConfigLoader, PathConfigLoader>();
        services.AddScoped<IFileRetentionService, FileRetentionService>();
        services.AddScoped<IFileTransferService, FileTransferService>();
        services.AddScoped<IEtapaConfigProvider, EtapaConfigProvider>();
        services.AddSingleton<IFileCompressor>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<StaSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<FileCompressor>>();
            return new FileCompressor(settings.Arquivo7Zip, logger);
        });

        // Worker support services
        services.AddScoped<IReturnDownloadService, ReturnDownloadService>();
        services.AddScoped<IWorkerPauseService, WorkerPauseService>();
        services.AddScoped<ITransferRouteProvider, TransferRouteProvider>();

        // Estado de execução (compartilhado entre Worker e API)
        services.AddSingleton<EstadoExecucao>();

        // Worker
        services.AddHostedService<Worker>();
    });

var host = builder.Build();
host.Run();
