namespace BackendProjectTemplate.Infrastructure.Authorization;

public interface IApplicationAuthorizationSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
