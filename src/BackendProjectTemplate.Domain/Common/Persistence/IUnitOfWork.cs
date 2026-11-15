namespace BackendProjectTemplate.Domain.Common.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards every tracked, unsaved change so a handler can recover from a rejected save (for example a
    /// database-enforced invariant) and persist a compensating outcome in the same unit of work.
    /// </summary>
    void DiscardChanges();
}
