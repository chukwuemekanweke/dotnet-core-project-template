using Microsoft.Extensions.DependencyInjection;
using AppDbContext = BackendProjectTemplate.Infrastructure.Persistence.AppDbContext;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

public sealed class ScopedDbContext(IServiceScope scope) : IDisposable
{
    public AppDbContext DbContext { get; } = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    public void Dispose() => scope.Dispose();
}
