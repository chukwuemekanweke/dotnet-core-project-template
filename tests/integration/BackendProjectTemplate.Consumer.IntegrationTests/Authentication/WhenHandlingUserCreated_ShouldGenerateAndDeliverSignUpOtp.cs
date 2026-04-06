using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Authentication;

[Collection(nameof(ContainersCollection))]
public sealed class WhenHandlingUserCreated_ShouldGenerateAndDeliverSignUpOtp(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string Password = "P@ssw0rd123!";
    private readonly ContainersFixture _fixture = fixture;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private Guid _userId;

    protected override Task InitializeWorkerTestAsync() => CreatePendingUserAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteAuthenticationRecordsAsync();

    [Fact]
    public async Task Verify()
    {
        await WhenPublishingUserCreated();
        await ThenTheSignUpOtpIsGeneratedAndDelivered();

        async Task WhenPublishingUserCreated()
        {
            var publisherConfig = new PublisherConfig
            {
                ServiceName = "BackendProjectTemplate.Consumer.IntegrationTests.Publisher",
                HostName = _fixture.RabbitMqHostName,
                Port = _fixture.RabbitMqPort,
                UserName = _fixture.RabbitMqUserName,
                Password = _fixture.RabbitMqPassword,
                VirtualHost = _fixture.RabbitMqVirtualHost,
                EventsExchange = "x.events.backendprojecttemplate.integrationtests"
            };

            await using var publisherServices = new ServiceCollection()
                .AddLogging()
                .AddPublisher(publisherConfig)
                .BuildServiceProvider();

            var publisher = publisherServices.GetRequiredKeyedService<IPublisher>(publisherConfig.Key);
            await publisher.PublishAsync(new UserCreated(_userId, _email), CancellationToken.None);
        }

        async Task ThenTheSignUpOtpIsGeneratedAndDelivered()
        {
            await WaitForConditionAsync(() =>
                Task.FromResult(!string.IsNullOrWhiteSpace(OtpDeliveryService.GetCode(_email))));

            var otpCode = OtpDeliveryService.GetCode(_email);
            otpCode.ShouldNotBeNull();

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
            var user = await repository.GetByEmailAsync(_email);

            user.ShouldNotBeNull();
            (await identityService.VerifySignUpOtpAsync(user, otpCode)).ShouldBeTrue();
        }
    }

    private async Task CreatePendingUserAsync()
    {
        _email = ConsumerIntegrationTestData.Email();
        _firstName = ConsumerIntegrationTestData.FirstName();
        _lastName = ConsumerIntegrationTestData.LastName();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var user = AppUser.Create(_email, _firstName, _lastName, timeProvider.GetUtcNow());

        var createResult = await identityService.CreateAsync(user, Password);
        createResult.Succeeded.ShouldBeTrue();
        _userId = user.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await repository.GetByEmailAsync(_email);

        if (user is null)
        {
            return;
        }

        repository.Remove(user);
        await unitOfWork.SaveChangesAsync();
    }
}
