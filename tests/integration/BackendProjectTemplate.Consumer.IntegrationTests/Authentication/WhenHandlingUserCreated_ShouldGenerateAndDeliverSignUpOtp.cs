using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AppDbContext = BackendProjectTemplate.Infrastructure.Persistence.AppDbContext;
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
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
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
            await publisher.PublishAsync(
                new UserCreated(_email)
                {
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId
                },
                CancellationToken.None,
                new Dictionary<string, string>
                {
                    [KnownMetadata.CorrelationId] = Guid.CreateVersion7().ToString("N")
                });
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

        _stakeholderId = Guid.CreateVersion7();
        _tenantId = Guid.CreateVersion7();
        _countryId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = AppUser.Create(_email, _firstName, _lastName, timeProvider.GetUtcNow());

        var createResult = await identityService.CreateAsync(user, Password);
        createResult.Succeeded.ShouldBeTrue();
        _userId = user.Id;

        var now = timeProvider.GetUtcNow();
        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual_customer", now);
        var stakeholder = Stakeholder.Create(_tenantId, _countryId, stakeholderType.Id, _firstName, _lastName, now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholder.Id, now);

        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.Stakeholders.AddAsync(stakeholder);
        await dbContext.AppUserStakeholders.AddAsync(appUserStakeholder);
        await dbContext.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
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
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await repository.GetByEmailAsync(_email);

        var appUserStakeholders = await dbContext.AppUserStakeholders
            .Where(link => link.AppUserId == _userId || link.StakeholderId == _stakeholderId)
            .ToListAsync();
        if (appUserStakeholders.Count > 0)
        {
            dbContext.AppUserStakeholders.RemoveRange(appUserStakeholders);
        }

        var stakeholders = await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == _stakeholderId)
            .ToListAsync();
        if (stakeholders.Count > 0)
        {
            dbContext.Stakeholders.RemoveRange(stakeholders);
        }

        var stakeholderTypes = await dbContext.StakeholderTypes
            .Where(stakeholderType => stakeholderType.Id == _stakeholderTypeId)
            .ToListAsync();
        if (stakeholderTypes.Count > 0)
        {
            dbContext.StakeholderTypes.RemoveRange(stakeholderTypes);
        }

        if (user is null)
        {
            await dbContext.SaveChangesAsync();
            return;
        }

        repository.Remove(user);
        await unitOfWork.SaveChangesAsync();
    }
}
