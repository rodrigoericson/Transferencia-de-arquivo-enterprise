using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public interface IEtapaRepository
{
    Task<IReadOnlyList<EtapaTransferencia>> BuscarEtapasAtivasAsync(
        int cnSistema, CancellationToken cancellationToken = default);
}
