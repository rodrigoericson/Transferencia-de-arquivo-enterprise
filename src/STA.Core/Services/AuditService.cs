using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;

namespace STA.Core.Services;

public class AuditService : IAuditService
{
    private readonly IAuditoriaRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditoriaRepository repository, ICurrentUser currentUser, ILogger<AuditService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task RegistrarAsync(
        string idEntidade,
        int idReferencia,
        string idAcao,
        string? detalhe = null,
        CancellationToken ct = default)
    {
        try
        {
            var auditoria = new Auditoria
            {
                CnUsuario = _currentUser.CnUsuario,
                NmUsuario = _currentUser.NmUsuario ?? "sistema",
                IdEntidade = idEntidade,
                IdReferencia = idReferencia,
                IdAcao = idAcao,
                DtAcao = DateTime.UtcNow,
                DsDetalhe = detalhe
            };

            await _repository.InserirAsync(auditoria, ct);
            _logger.LogDebug("Auditoria registrada: {Entidade} {Acao} ref={IdRef} por {Usuario}",
                idEntidade, idAcao, idReferencia, auditoria.NmUsuario);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao registrar auditoria: {Entidade}/{Acao} ref={IdRef}",
                idEntidade, idAcao, idReferencia);
        }
    }
}
