using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public interface ILogSftpRepository
{
    Task InserirAsync(LogSftp log, CancellationToken ct = default);
}
