using System.Text.Json;
using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using AppDbContext = BackendProjectTemplate.Infrastructure.Persistence.AppDbContext;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Authentication;

[Collection(nameof(ContainersCollection))]
public sealed class WhenHandlingUserSignInSuccessful_ShouldResetFailedCountAndQueueSignInSuccessfulNotificationCommand(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string Password = "P@ssw0rd123!";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ContainersFixture _fixture = fixture;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _ipAddress = string.Empty;
    private string _userAgent = string.Empty;
    private Guid _userId;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;

    protected override Task InitializeWorkerTestAsync() => SeedUserAndStakeholderAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteSeedDataAsync();

    [Fact]
    public async Task Verify()
    {
        await WhenPublishingUserSignInSuccessful();
        await ThenTheFailedCountIsResetAndTheNotificationCommandIsQueued();

        async Task WhenPublishingUserSignInSuccessful()
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
            await publisher.PublishAsync(new UserSignInSuccessful(_userId, _email, _ipAddress, _userAgent), CancellationToken.None);
        }

        async Task ThenTheFailedCountIsResetAndTheNotificationCommandIsQueued()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                var user = await scope.DbContext.Users.SingleAsync(candidate => candidate.Id == _userId);
                var command = await scope.DbContext.OutboxMessages
                    .Where(message =>
                        message.Kind == OutboxMessageKind.Command &&
                        message.Type == typeof(SendNotificationCommand).FullName! &&
                        message.Payload.Contains(_email))
                    .OrderByDescending(message => message.EnqueuedAtUtc)
                    .FirstOrDefaultAsync();

                return user.AccessFailedCount == 0 && command is not null;
            });

            using var assertionScope = CreateDbContextScope();
            var user = await assertionScope.DbContext.Users.SingleAsync(candidate => candidate.Id == _userId);
            var outboxMessage = await assertionScope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(SendNotificationCommand).FullName! &&
                    message.Payload.Contains(_email))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstAsync();

            var command = JsonSerializer.Deserialize<SendNotificationCommand>(outboxMessage.Payload, SerializerOptions);

            user.AccessFailedCount.ShouldBe(0);
            command.ShouldNotBeNull();
            command.TenantId.ShouldBe(_tenantId);
            command.CountryId.ShouldBe(_countryId);
            command.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
            command.NotificationMedium.ShouldBe(NotificationMedium.Email);
            command.NotificationContent.ShouldBeOfType<EmailNotificationContent>();
            ((EmailNotificationContent)command.NotificationContent).To.ShouldBe(_email);
        }
    }

    private async Task SeedUserAndStakeholderAsync()
    {
        _email = ConsumerIntegrationTestData.Email();
        _firstName = ConsumerIntegrationTestData.FirstName();
        _lastName = ConsumerIntegrationTestData.LastName();
        _ipAddress = ConsumerIntegrationTestData.IpAddress();
        _userAgent = ConsumerIntegrationTestData.UserAgent();
        _tenantId = Guid.CreateVersion7();
        _countryId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(_email, _firstName, _lastName, now);

        var createResult = await identityService.CreateAsync(user, Password);
        createResult.Succeeded.ShouldBeTrue();

        user.AccessFailedCount = 3;
        user.LockoutEnabled = true;

        var updateResult = await identityService.UpdateAsync(user);
        updateResult.Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual_customer", now);
        var stakeholder = Stakeholder.Create(_tenantId, _countryId, stakeholderType.Id, now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholder.Id, now);

        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.Stakeholders.AddAsync(stakeholder);
        await dbContext.AppUserStakeholders.AddAsync(appUserStakeholder);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateDbContextScope();
        var dbContext = scope.DbContext;

        var outboxMessages = await dbContext.OutboxMessages
            .Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(SendNotificationCommand).FullName! &&
                message.Payload.Contains(_email))
            .ToListAsync();

        if (outboxMessages.Count > 0)
        {
            dbContext.OutboxMessages.RemoveRange(outboxMessages);
        }

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

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == _userId);
        if (user is not null)
        {
            dbContext.Users.Remove(user);
        }

        await dbContext.SaveChangesAsync();
    }
}
