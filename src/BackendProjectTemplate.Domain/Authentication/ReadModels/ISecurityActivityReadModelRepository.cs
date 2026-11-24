namespace BackendProjectTemplate.Domain.Authentication.ReadModels;

public interface ISecurityActivityReadModelRepository
{
    Task<SecurityActivityHistoryCursorPage> GetByStakeholderAsync(
        SecurityActivityHistoryCursorRequest request,
        CancellationToken cancellationToken);
}
