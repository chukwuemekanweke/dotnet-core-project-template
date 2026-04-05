using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public abstract class WebApiIntegrationTestBase : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    protected WebApiIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture.SqlConnectionString, fixture.RedisConnectionString);
    }

    protected HttpClient Client { get; private set; } = default!;
    protected TestOtpDeliveryService OtpDeliveryService => _factory.OtpDeliveryService;

    public virtual Task InitializeAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
