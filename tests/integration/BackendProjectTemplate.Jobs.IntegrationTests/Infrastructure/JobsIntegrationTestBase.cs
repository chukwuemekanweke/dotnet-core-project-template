using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;

public abstract class JobsIntegrationTestBase
{
    private readonly CustomJobsApplicationFactory _factory;

    protected JobsIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomJobsApplicationFactory(
            fixture.SqlConnectionString,
            fixture.RedisConnectionString,
            fixture.RabbitMqHostName,
            fixture.RabbitMqPort,
            fixture.RabbitMqUserName,
            fixture.RabbitMqPassword,
            fixture.RabbitMqVirtualHost);
    }

    protected HttpClient Client { get; private set; } = default!;

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

    protected static async Task WaitForHealthyAsync(Func<Task<HttpResponseMessage>> probe)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await probe();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new InvalidOperationException("The health endpoint did not become healthy in time.");
    }

    protected static async Task WaitForConditionAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException("The expected condition was not met in time.");
    }
}
