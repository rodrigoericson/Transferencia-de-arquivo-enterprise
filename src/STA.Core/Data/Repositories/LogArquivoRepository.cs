using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public class LogArquivoRepository : ILogArquivoRepository
{
    private readonly StaDbContext _context;

    public LogArquivoRepository(StaDbContext context)
    {
        _context = context;
    }

    public async Task InserirAsync(LogArquivo log, CancellationToken cancellationToken = default)
    {
        _context.LogArquivos.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
