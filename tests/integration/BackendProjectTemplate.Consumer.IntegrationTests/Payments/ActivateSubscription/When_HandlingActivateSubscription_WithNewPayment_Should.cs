using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Payments.ActivateSubscription;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingActivateSubscription_WithNewPayment_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentTransactionId;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _subscriptionActivationId;
    private Guid _billingAccountId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override async Task InitializeWorkerTestAsync()
    {
        _paymentTransactionId = Guid.CreateVersion7();
        _tenantId = Guid.CreateVersion7();
        _currencyId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var tenant = Tenant.Create(_tenantId, "Subscription Tenant", $"subscription-{_tenantId:N}");
        var user = AppUser.Create($"subscription-{Guid.CreateVersion7():N}@example.com", "Subscription", "Owner");
        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual-customer");
        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Subscription",
            "Owner");

        dbContext.Tenants.Add(tenant);
        dbContext.StakeholderTypes.Add(stakeholderType);
        dbContext.Stakeholders.Add(stakeholder);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.SubscriptionActivations
            .Where(item => item.Id == _subscriptionActivationId)
            .ExecuteDeleteAsync();
        await scope.DbContext.BillingAccounts
            .Where(item => item.Id == _billingAccountId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Stakeholders
            .Where(item => item.Id == _stakeholderId)
            .ExecuteDeleteAsync();
        await scope.DbContext.StakeholderTypes
            .Where(item => item.Id == _stakeholderTypeId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Users
            .Where(item => item.Id == _userId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Tenants
            .Where(item => item.Id == _tenantId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task CreateSubscriptionActivation()
    {
        await WhenPublishingActivateSubscriptionCommand();
        await ThenTheSubscriptionActivationIsCreated();

        async Task WhenPublishingActivateSubscriptionCommand()
        {
            using var scope = CreateScope();
            var messageContext = scope.ServiceProvider.GetRequiredService<Chidelu.Integration.Messaging.RabbitMQ.Consumer.IMessageContext>();

            await scope.ServiceProvider.GetRequiredService<ActivateSubscriptionHandler>().HandleAsync(
                new ActivateSubscriptionCommand(_paymentTransactionId, "merchant-ref", 5000m, _currencyId)
                {
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId,
                    FlowId = "flow-123"
                },
                CancellationToken.None);
        }

        async Task ThenTheSubscriptionActivationIsCreated()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                return await scope.DbContext.SubscriptionActivations.AnyAsync(item => item.PaymentTransactionId == _paymentTransactionId);
            });

            using var scope = CreateDbContextScope();
            var activation = await scope.DbContext.SubscriptionActivations.FirstAsync(item => item.PaymentTransactionId == _paymentTransactionId);

            _subscriptionActivationId = activation.Id;
            _billingAccountId = activation.BillingAccountId;
            activation.StakeholderId.ShouldBe(_stakeholderId);
            activation.TenantId.ShouldBe(_tenantId);
            activation.Amount.ShouldBe(5000m);
            var billingAccount = await scope.DbContext.BillingAccounts.SingleAsync(item => item.Id == activation.BillingAccountId);
            billingAccount.StakeholderId.ShouldBe(_stakeholderId);
        }
    }
}
