using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public class LogSftpRepository : ILogSftpRepository
{
    private readonly StaDbContext _context;

    public LogSftpRepository(StaDbContext context)
    {
        _context = context;
    }

    public async Task InserirAsync(LogSftp log, CancellationToken ct = default)
    {
        _context.LogsSftp.Add(log);
        await _context.SaveChangesAsync(ct);
    }
}
