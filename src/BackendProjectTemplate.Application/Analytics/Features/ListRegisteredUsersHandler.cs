using BackendProjectTemplate.Domain.Analytics.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed class ListRegisteredUsersHandler(
    IRegistrationAnalyticsReadModelRepository repository,
    TimeProvider timeProvider,
    IOptions<AdministrativeAnalyticsOptions> options)
{
    private readonly AdministrativeAnalyticsOptions _options = options.Value;

    public Task<RegisteredUserListPage> HandleAsync(
        RegistrationListQuery query,
        CancellationToken cancellationToken)
    {
        var to = query.To ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = query.From ?? to.AddDays(-(_options.DefaultRangeDays - 1));
        return repository.ListAsync(
            new RegisteredUserListRequest(
                StartOfDay(from),
                StartOfDay(to.AddDays(1)),
                query.TenantId,
                query.Page,
                query.PageSize),
            cancellationToken);
    }

    private static DateTimeOffset StartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

}
