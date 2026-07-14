using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public interface ILogArquivoRepository
{
    Task InserirAsync(LogArquivo log, CancellationToken cancellationToken = default);
}
