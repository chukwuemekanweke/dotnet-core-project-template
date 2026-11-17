using BackendProjectTemplate.Application.Kyc.Features.ReconcileVerifications;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using BackendProjectTemplate.Jobs.Kyc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests.Kyc.Reconciliation;

public sealed class When_ReconcilingKycVerifications_WithConfiguredSchedule_Should
{
    [Fact]
    public async Task InvokeReconciliationServiceWithConfiguredWindow()
    {
        var verificationRepository = Substitute.For<IRepository<KycVerification>>();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(KycReconciliationProcessor.ServiceName)]);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 15, 0, 0, TimeSpan.Zero));
        var services = new ServiceCollection()
            .AddSingleton(verificationRepository)
            .AddSingleton(Substitute.For<IRepository<Stakeholder>>())
            .AddSingleton<IEnumerable<IKycProviderService>>([Substitute.For<IKycProviderService>()])
            .AddSingleton(Substitute.For<IEventPublisher>())
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .AddSingleton<TimeProvider>(clock)
            .AddLogging()
            .AddScoped<KycReconciliationService>()
            .BuildServiceProvider();
        var listAsyncCalled = new TaskCompletionSource<ISpecification<KycVerification>>(TaskCreationOptions.RunContinuationsAsynchronously);
        verificationRepository.ListAsync(Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                listAsyncCalled.TrySetResult(callInfo.Arg<ISpecification<KycVerification>>());
                return new List<KycVerification>();
            });
        var sut = new KycReconciliationProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KycReconciliationOptions
            {
                BatchSize = 10,
                PollInterval = TimeSpan.FromSeconds(1),
                StaleThreshold = TimeSpan.FromMinutes(30),
                RecheckDelay = TimeSpan.FromMinutes(30)
            }),
            state,
            clock,
            NullLogger<KycReconciliationProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        var specification = await listAsyncCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        state.IsReady.ShouldBeTrue();
        specification.ShouldBeOfType<PendingKycReconciliationSpecification>();
        specification.Take.ShouldBe(10);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
