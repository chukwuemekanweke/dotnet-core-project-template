using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

/// <summary>
/// Seeds an administrator (in its own tenant), a target tenant with a stakeholder, a stakeholder in an
/// unrelated tenant, a currency and an active plan, then removes every record those tenants own.
/// </summary>
public abstract class AdminSubscriptionIntegrationTestBase(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    protected const string Password = "P@ssw0rd123!";

    private readonly List<Guid> _tenantIds = [];
    private readonly List<string> _emails = [];
    private string _email = string.Empty;
    private Guid _countryId;
    private readonly List<Guid> _stakeholderTypeIds = [];
    private readonly List<Guid> _paymentProviderIds = [];

    protected Guid AdminUserId { get; private set; }
    protected Guid AdminTenantId { get; private set; }
    protected Guid AdminStakeholderId { get; private set; }
    protected Guid TargetTenantId { get; private set; }
    protected Guid TargetStakeholderId { get; private set; }
    protected Guid ForeignTenantId { get; private set; }
    protected Guid ForeignStakeholderId { get; private set; }
    protected Guid CurrencyId { get; private set; }
    protected Guid PlanId { get; private set; }
    protected string PlanCodePrefix { get; } = $"IT{Guid.CreateVersion7():N}"[..18].ToUpperInvariant();

    public virtual async Task InitializeAsync()
    {
        await InitializeClientAsync();
        AdminTenantId = Guid.CreateVersion7();
        TargetTenantId = Guid.CreateVersion7();
        ForeignTenantId = Guid.CreateVersion7();
        _tenantIds.AddRange([AdminTenantId, TargetTenantId, ForeignTenantId]);
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", AdminTenantId.ToString());
        await SeedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subscriptionIds = await dbContext.Subscriptions
            .Where(item => _tenantIds.Contains(item.TenantId))
            .Select(item => item.Id)
            .ToListAsync();
        var renewalIds = await dbContext.SubscriptionRenewals
            .Where(item => subscriptionIds.Contains(item.SubscriptionId))
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var tenantId in _tenantIds)
        {
            var tenantText = tenantId.ToString();
            await dbContext.OutboxMessages.Where(item => item.Payload.Contains(tenantText)).ExecuteDeleteAsync();
        }

        await dbContext.SubscriptionAllocations.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionRenewalAttempts.Where(item => renewalIds.Contains(item.RenewalId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionRenewals.Where(item => renewalIds.Contains(item.Id)).ExecuteDeleteAsync();
        await dbContext.SubscriptionActivations.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionStatusHistory.Where(item => subscriptionIds.Contains(item.SubscriptionId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionPeriods.Where(item => subscriptionIds.Contains(item.SubscriptionId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionPurchases.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.Subscriptions.Where(item => subscriptionIds.Contains(item.Id)).ExecuteDeleteAsync();
        await dbContext.PaymentTransactions.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.PaymentProviders.IgnoreQueryFilters()
            .Where(item => _paymentProviderIds.Contains(item.Id))
            .ExecuteDeleteAsync();
        await dbContext.BillingAccounts.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.SubscriptionPlanPrices.IgnoreQueryFilters()
            .Where(price => dbContext.SubscriptionPlans.IgnoreQueryFilters()
                .Any(plan => plan.Id == price.PlanId && plan.Code.StartsWith(PlanCodePrefix)))
            .ExecuteDeleteAsync();
        await dbContext.SubscriptionPlans.IgnoreQueryFilters().Where(item => item.Code.StartsWith(PlanCodePrefix)).ExecuteDeleteAsync();
        await dbContext.Currencies.Where(item => item.Id == CurrencyId).ExecuteDeleteAsync();
        await dbContext.Stakeholders.Where(item => _tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await dbContext.StakeholderTypes.Where(item => _stakeholderTypeIds.Contains(item.Id)).ExecuteDeleteAsync();
        await dbContext.Countries.Where(item => item.Id == _countryId).ExecuteDeleteAsync();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        foreach (var email in _emails)
        {
            var user = await userRepository.GetByEmailAsync(email);
            if (user is not null)
            {
                userRepository.Remove(user);
            }
        }

        await dbContext.SaveChangesAsync();

        await dbContext.Tenants.Where(item => _tenantIds.Contains(item.Id)).ExecuteDeleteAsync();
        await DisposeClientAsync();
    }

    protected async Task AuthenticateAsync()
    {
        using var response = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(AdminUserId.ToString());
        (await userManager.SetTwoFactorEnabledAsync(user!, true)).Succeeded.ShouldBeTrue();
    }

    protected async Task GrantRoleAsync(string role)
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(AdminUserId.ToString());
        (await userManager.AddToRoleAsync(user!, role)).Succeeded.ShouldBeTrue();
    }

    protected async Task RevokeRoleAsync(string role)
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(AdminUserId.ToString());
        (await userManager.RemoveFromRoleAsync(user!, role)).Succeeded.ShouldBeTrue();
    }

    protected AllocateSubscriptionRequest TenantAllocation(
        DateTimeOffset? startAtUtc = null,
        DateTimeOffset? endAtUtc = null,
        Guid? idempotencyKey = null,
        string source = "AdminAllocation") =>
        new(
            TargetTenantId,
            "Tenant",
            null,
            PlanId,
            startAtUtc,
            endAtUtc,
            source,
            "Integration allocation",
            idempotencyKey ?? Guid.CreateVersion7());

    protected AllocateSubscriptionRequest StakeholderAllocation(Guid stakeholderId, DateTimeOffset? startAtUtc = null, DateTimeOffset? endAtUtc = null) =>
        new(
            TargetTenantId,
            "Stakeholder",
            stakeholderId,
            PlanId,
            startAtUtc,
            endAtUtc,
            "Promotion",
            "Integration promotion",
            Guid.CreateVersion7());

    protected Task<HttpResponseMessage> AllocateAsync(AllocateSubscriptionRequest request) =>
        Client.PostAsJsonAsync(EndpointUrl.AdminSubscriptions.AllocationsV1, request);

    protected async Task<AllocateSubscriptionResponse> AllocateSuccessfullyAsync(AllocateSubscriptionRequest request)
    {
        using var response = await AllocateAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AllocateSubscriptionResponse>())!;
    }

    protected async Task<SubscriptionPlan> CreatePlanAsync(string suffix, decimal price, BillingInterval interval, bool isActive)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = SubscriptionPlan.Create($"{PlanCodePrefix}_{suffix}", $"Plan {suffix}", price, CurrencyId, interval, isActive);
        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    protected DateTimeOffset UtcNow()
    {
        using var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
    }

    protected void TrackPaymentProviderForCleanup(Guid paymentProviderId) =>
        _paymentProviderIds.Add(paymentProviderId);

    private async Task SeedAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var users = new List<AppUser>();
        foreach (var _ in _tenantIds)
        {
            var email = WebApiIntegrationTestData.Email();
            var tenantUser = AppUser.Create(email, "Grace", "Hopper");
            (await identityService.CreateAsync(tenantUser, Password)).Succeeded.ShouldBeTrue();
            tenantUser.MarkEmailVerified();
            (await identityService.UpdateAsync(tenantUser)).Succeeded.ShouldBeTrue();
            _emails.Add(email);
            users.Add(tenantUser);
        }

        var user = users[0];
        _email = _emails[0];

        var country = Country.Create("Admin Subscription Country", "ASC", "+998", "https://example.com/asc.svg");
        var currency = Currency.Create(Guid.CreateVersion7().ToString("N")[..3].ToUpperInvariant(), "Admin Test Currency", true);
        dbContext.Countries.Add(country);
        dbContext.Currencies.Add(currency);
        var stakeholderIds = new List<Guid>();
        for (var index = 0; index < _tenantIds.Count; index++)
        {
            var tenantId = _tenantIds[index];
            dbContext.Tenants.Add(Tenant.Create(tenantId, $"Tenant {tenantId:N}"[..20], $"admin-sub-{tenantId:N}"));
            var type = StakeholderType.Create(tenantId, "Customer", "customer");
            _stakeholderTypeIds.Add(type.Id);
            dbContext.StakeholderTypes.Add(type);
            var stakeholder = Stakeholder.Create(users[index].Id, tenantId, country.Id, type.Id, "Grace", "Hopper");
            dbContext.Stakeholders.Add(stakeholder);
            stakeholderIds.Add(stakeholder.Id);
        }

        var plan = SubscriptionPlan.Create($"{PlanCodePrefix}_BASE", "Integration Base", 750000m, currency.Id, BillingInterval.Monthly, true);
        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        AdminUserId = user.Id;
        _countryId = country.Id;
        CurrencyId = currency.Id;
        PlanId = plan.Id;
        AdminStakeholderId = stakeholderIds[0];
        TargetStakeholderId = stakeholderIds[1];
        ForeignStakeholderId = stakeholderIds[2];
    }
}
