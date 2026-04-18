using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public abstract class WebApiIntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;

    protected WebApiIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture.SqlConnectionString, fixture.RedisConnectionString);
    }

    protected HttpClient Client { get; private set; } = default!;
    protected FakeGoogleIdentityTokenService GoogleIdentityTokenService => _factory.GoogleIdentityTokenService;

    protected Task InitializeClientAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return Task.CompletedTask;
    }

    protected async Task DisposeClientAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected IServiceScope CreateScope() => _factory.Services.CreateScope();
}
