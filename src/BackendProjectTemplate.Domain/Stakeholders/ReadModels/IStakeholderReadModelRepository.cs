namespace BackendProjectTemplate.Domain.Stakeholders.ReadModels;

public interface IStakeholderReadModelRepository
{
    Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default);
}
