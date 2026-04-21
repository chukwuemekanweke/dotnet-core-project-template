using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Jobs.Authentication;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BackendProjectTemplate.Jobs.UnitTests.Authentication.IpAddressLocationEnrichment;

public sealed class When_EnrichingIpAddresses_WithNoPendingRecords_Should
{
    [Fact]
    public async Task SkipSavingChanges()
    {
        var repository = Substitute.For<IRepository<IpAddress>>();
        var geolocationService = Substitute.For<IIpGeolocationService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(IpAddressLocationEnrichmentProcessor.ServiceName)]);
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(geolocationService)
            .AddSingleton(unitOfWork)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<IpAddress>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IpAddress>());

        var sut = new IpAddressLocationEnrichmentProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new IpAddressLocationEnrichmentOptions { BatchSize = 10, PollIntervalSeconds = 1, LocationRefreshIntervalDays = 30 }),
            state,
            NullLogger<IpAddressLocationEnrichmentProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
