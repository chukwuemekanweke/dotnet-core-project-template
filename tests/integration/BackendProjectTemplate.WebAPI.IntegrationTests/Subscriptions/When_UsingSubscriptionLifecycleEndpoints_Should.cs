using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;
using BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;
using BackendProjectTemplate.Application.Subscriptions.Features.ListEligiblePaymentProviders;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_UsingSubscriptionLifecycleEndpoints_Should(ContainersFixture fixture) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private readonly CustomWebApplicationFactory _factory = new(
        fixture.PostgresConnectionString,
        fixture.RedisConnectionString);
    private HttpClient _client = default!;
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _userId;
    private Guid _countryId;
    private Guid _stakeholderTypeId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _countryCurrencyId;
    private Guid _providerId;
    private Guid _providerConfigurationId;
    private Guid _ineligibleProviderId;
    private Guid _ineligibleProviderConfigurationId;
    private Guid _planId;
    private Guid _inactivePlanId;
    private Guid _subscriptionId;
    private Guid _purchaseId;
    private Guid _paymentTransactionId;
    private Guid _purchaseIdempotencyKey;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _tenantId = Guid.CreateVersion7();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await SeedActorAndCatalogAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteSeedDataAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ListPurchaseQueryAndCancelSubscription()
    {
        await ThenPlansCanBeListedAnonymously();
        await ThenPlanCanBeListed();
        await ThenEligiblePaymentProvidersCanBeListed();
        await WhenPurchasingPlan();
        await ThenPurchaseRetriesAreIdempotent();
        await ThenPurchaseIsPending();
        await ThenPendingSubscriptionCanBeQueried();
        await GivenPurchaseIsFulfilled();
        await ThenPurchaseIsActiveAndBillingHistoryIsAvailable();
        await ThenCancellationCanBeScheduled();
    }

    private async Task ThenPlansCanBeListedAnonymously()
    {
        using var anonymousClient = _factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        using var response = await anonymousClient.GetAsync(EndpointUrl.SubscriptionPlans.V1);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var plans = JsonSerializer.Deserialize<IReadOnlyList<ListSubscriptionPlansResponse>>(
            content,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        plans.ShouldNotBeNull();
        plans.ShouldContain(plan => plan.Id == _planId);
        plans.ShouldNotContain(plan => plan.Id == _inactivePlanId);
        content.ShouldNotContain("isActive", Case.Insensitive);
    }

    private async Task ThenPlanCanBeListed()
    {
        using var response = await _client.GetAsync(EndpointUrl.SubscriptionPlans.V1);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<IReadOnlyList<ListSubscriptionPlansResponse>>();
        plans.ShouldNotBeNull();
        var listed = plans.Single(plan => plan.Id == _planId);
        listed.CurrencyId.ShouldBe(_currencyId);
        listed.Prices.Select(price => (price.BillingInterval, price.Price)).ShouldBe([("Monthly", 750000m), ("Yearly", 7500000m)]);
        listed.Description.ShouldBe("API test plan.");
        listed.Features.ShouldBe(["Core features"]);
        plans.ShouldNotContain(plan => plan.Id == _inactivePlanId);
    }

    private async Task WhenPurchasingPlan()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.Subscriptions.PurchasesV1)
        {
            Content = JsonContent.Create(new PurchaseSubscriptionRequest(_planId, "Monthly", _providerId, "Stakeholder", true))
        };
        _purchaseIdempotencyKey = Guid.CreateVersion7();
        request.Headers.Add("Idempotency-Key", _purchaseIdempotencyKey.ToString());
        using var response = await _client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PurchaseSubscriptionResponse>();
        payload.ShouldNotBeNull();

        _subscriptionId = payload.SubscriptionId;
        _purchaseId = payload.PurchaseId;

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var purchase = await dbContext.SubscriptionPurchases.SingleAsync(item => item.Id == _purchaseId);
        var payment = await dbContext.PaymentTransactions.SingleAsync(item => item.Id == purchase.PaymentTransactionId);
        _paymentTransactionId = payment.Id;
        purchase.ExpectedAmount.ShouldBe(750000m);
        purchase.ExpectedCurrencyId.ShouldBe(_currencyId);
        purchase.BillingAccountId.ShouldNotBe(Guid.Empty);
        payment.Amount.ShouldBe(750000m);
        payment.CurrencyId.ShouldBe(_currencyId);
    }

    private async Task ThenPurchaseRetriesAreIdempotent()
    {
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.Subscriptions.PurchasesV1)
        {
            Content = JsonContent.Create(new PurchaseSubscriptionRequest(_planId, "Monthly", _providerId, "Stakeholder", true))
        };
        replayRequest.Headers.Add("Idempotency-Key", _purchaseIdempotencyKey.ToString());
        using var replayResponse = await _client.SendAsync(replayRequest);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = await replayResponse.Content.ReadFromJsonAsync<PurchaseSubscriptionResponse>();
        replay.ShouldNotBeNull();
        replay.PurchaseId.ShouldBe(_purchaseId);
        replay.SubscriptionId.ShouldBe(_subscriptionId);

        using var changedRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.Subscriptions.PurchasesV1)
        {
            Content = JsonContent.Create(new PurchaseSubscriptionRequest(_planId, "Yearly", _providerId, "Stakeholder", true))
        };
        changedRequest.Headers.Add("Idempotency-Key", _purchaseIdempotencyKey.ToString());
        using var changedResponse = await _client.SendAsync(changedRequest);
        changedResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private async Task ThenEligiblePaymentProvidersCanBeListed()
    {
        using var response = await _client.GetAsync(
            $"{EndpointUrl.SubscriptionPlans.V1}/{_planId}/payment-providers?BillingAccountOwnerType=Stakeholder");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<IReadOnlyList<EligiblePaymentProviderResponse>>();
        providers.ShouldNotBeNull();
        providers.ShouldContain(provider =>
            provider.Id == _providerId &&
            provider.PaymentMethodType == Contracts.Payments.PaymentMethodType.PaymentLink.ToString() &&
            provider.SupportsRecurringPayments);
        providers.ShouldNotContain(provider => provider.Id == _ineligibleProviderId);
    }

    private async Task ThenPurchaseIsPending()
    {
        using var response = await _client.GetAsync(
            $"{EndpointUrl.Subscriptions.PurchaseStatusV1(_purchaseId)}?BillingAccountOwnerType=Stakeholder");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<GetSubscriptionPurchaseStatusResponse>();
        status.ShouldNotBeNull();
        status.Status.ShouldBe("PaymentPending");
        status.ShouldContinuePolling.ShouldBeTrue();

        using var unauthorizedResponse = await _client.GetAsync(
            $"{EndpointUrl.Subscriptions.PurchaseStatusV1(_purchaseId)}?BillingAccountOwnerType=Tenant");
        unauthorizedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task ThenPendingSubscriptionCanBeQueried()
    {
        using var response = await _client.GetAsync(
            $"{EndpointUrl.Subscriptions.CurrentV1}?BillingAccountOwnerType=Stakeholder");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var current = await response.Content.ReadFromJsonAsync<GetCurrentSubscriptionResponse>();
        current.ShouldNotBeNull();
        current.Id.ShouldBe(_subscriptionId);
        current.Status.ShouldBe(SubscriptionStatus.Pending.ToString());
        current.LifecycleStatus.ShouldBe(SubscriptionStatus.Pending.ToString());
        current.PlanPrice.ShouldBe(750000m);
        current.BillingInterval.ShouldBe(nameof(BillingInterval.Monthly));
    }

    private async Task GivenPurchaseIsFulfilled()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lifecyclePersistence = scope.ServiceProvider.GetRequiredService<ISubscriptionLifecyclePersistence>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var subscription = await dbContext.Subscriptions
            .Include(item => item.Periods)
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == _subscriptionId);
        var purchase = await dbContext.SubscriptionPurchases.SingleAsync(item => item.Id == _purchaseId);
        var payment = await dbContext.PaymentTransactions.SingleAsync(item => item.Id == _paymentTransactionId);
        var now = timeProvider.GetUtcNow();
        payment.MarkSucceeded("subscription-api-test", "confirmed", now);
        var period = subscription.ActivateFromPayment(
            purchase,
            _paymentTransactionId,
            now,
            purchase.Id);
        var statusHistory = subscription.StatusHistory.Single(item => item.OperationId == purchase.Id);
        await lifecyclePersistence.AddPeriodAsync(period, CancellationToken.None);
        await lifecyclePersistence.AddStatusHistoryAsync(statusHistory, CancellationToken.None);
        purchase.MarkFulfilled(now);
        await dbContext.SaveChangesAsync();
    }

    private async Task ThenCancellationCanBeScheduled()
    {
        using var response = await _client.PostAsJsonAsync(
            EndpointUrl.Subscriptions.CurrentCancellationV1,
            new SubscriptionOwnerRequest("Stakeholder"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CancelSubscriptionAtPeriodEndResult>();
        result.ShouldNotBeNull();
        result.Found.ShouldBeTrue();
        result.Changed.ShouldBeTrue();
        result.EntitledUntilUtc.ShouldNotBeNull();

        using var scope = CreateScope();
        var subscription = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Subscriptions.SingleAsync(item => item.Id == _subscriptionId);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CancelAtPeriodEnd.ShouldBeTrue();
        subscription.AutoRenew.ShouldBeFalse();
        subscription.NextBillingAtUtc.ShouldBeNull();
    }

    private async Task ThenPurchaseIsActiveAndBillingHistoryIsAvailable()
    {
        using (var response = await _client.GetAsync(
                   $"{EndpointUrl.Subscriptions.PurchaseStatusV1(_purchaseId)}?BillingAccountOwnerType=Stakeholder"))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var status = await response.Content.ReadFromJsonAsync<GetSubscriptionPurchaseStatusResponse>();
            status.ShouldNotBeNull();
            status.Status.ShouldBe("SubscriptionActive");
            status.IsTerminal.ShouldBeTrue();
        }

        using (var response = await _client.GetAsync(
                   $"{EndpointUrl.Subscriptions.BillingHistoryV1}?BillingAccountOwnerType=Stakeholder&Page=1&PageSize=20"))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var history = await response.Content.ReadFromJsonAsync<GetBillingHistoryResponse>();
            history.ShouldNotBeNull();
            history.TotalCount.ShouldBe(1);
            history.Items.Single().PaymentTransactionId.ShouldBe(_paymentTransactionId);
            history.Items.Single().Amount.ShouldBe(750000m);
        }
    }

    private async Task AuthenticateAsync()
    {
        using var response = await _client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task SeedActorAndCatalogAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _email = WebApiIntegrationTestData.Email();
        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var tenant = Tenant.Create(_tenantId, "Subscription API Tenant", $"subscription-api-{_tenantId:N}");
        var country = Country.Create("Subscription Country", "SCT", "+999", "https://example.com/sct.svg");
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, country.Id, stakeholderType.Id, "Ada", "Lovelace");
        var currency = Currency.Create(Guid.CreateVersion7().ToString("N")[..3].ToUpperInvariant(), "Test Currency", true);
        var countryCurrency = CountryCurrency.Create(country.Id, currency.Id, true, true);
        var provider = PaymentProvider.Create("Bachs", PaymentProviderKeys.Bachs, true);
        provider.SetConfiguration(currency.Id, Contracts.Payments.PaymentIntent.Subscription, Contracts.Payments.PaymentMethodType.PaymentLink, true);
        var ineligibleProvider = PaymentProvider.Create("Disabled Provider", "disabled-provider", true);
        ineligibleProvider.SetConfiguration(
            currency.Id,
            Contracts.Payments.PaymentIntent.Subscription,
            Contracts.Payments.PaymentMethodType.Card,
            false);
        var plan = SubscriptionPlan.Create(
            $"API_{Guid.CreateVersion7():N}",
            "API Plan",
            currency.Id,
            new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 750000m, [BillingInterval.Yearly] = 7500000m },
            true,
            "API test plan.",
            ["Core features"]);
        var inactivePlan = SubscriptionPlan.Create(
            $"INACTIVE_{Guid.CreateVersion7():N}",
            "Inactive API Monthly",
            500000m,
            currency.Id,
            BillingInterval.Monthly,
            false);

        dbContext.Tenants.Add(tenant);
        dbContext.Countries.Add(country);
        dbContext.StakeholderTypes.Add(stakeholderType);
        dbContext.Stakeholders.Add(stakeholder);
        dbContext.Currencies.Add(currency);
        dbContext.CountryCurrencies.Add(countryCurrency);
        dbContext.PaymentProviders.Add(provider);
        dbContext.PaymentProviders.Add(ineligibleProvider);
        dbContext.SubscriptionPlans.Add(plan);
        dbContext.SubscriptionPlans.Add(inactivePlan);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
        _currencyId = currency.Id;
        _countryCurrencyId = countryCurrency.Id;
        _providerId = provider.Id;
        _providerConfigurationId = provider.Configurations.Single().Id;
        _ineligibleProviderId = ineligibleProvider.Id;
        _ineligibleProviderConfigurationId = ineligibleProvider.Configurations.Single().Id;
        _planId = plan.Id;
        _inactivePlanId = inactivePlan.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        await dbContext.SubscriptionStatusHistory.Where(item => item.SubscriptionId == _subscriptionId).ExecuteDeleteAsync();
        await dbContext.SubscriptionPeriods.Where(item => item.SubscriptionId == _subscriptionId).ExecuteDeleteAsync();
        await dbContext.SubscriptionPurchases.Where(item => item.Id == _purchaseId).ExecuteDeleteAsync();
        await dbContext.Subscriptions.Where(item => item.Id == _subscriptionId).ExecuteDeleteAsync();
        await dbContext.PaymentTransactions.Where(item => item.Id == _paymentTransactionId).ExecuteDeleteAsync();
        await dbContext.BillingAccounts.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await dbContext.SubscriptionPlanPrices.Where(item => item.PlanId == _planId || item.PlanId == _inactivePlanId).ExecuteDeleteAsync();
        await dbContext.SubscriptionPlans.Where(item => item.Id == _planId).ExecuteDeleteAsync();
        await dbContext.SubscriptionPlans.Where(item => item.Id == _inactivePlanId).ExecuteDeleteAsync();
        await dbContext.PaymentProviderConfigurations.Where(item => item.Id == _providerConfigurationId).ExecuteDeleteAsync();
        await dbContext.PaymentProviderConfigurations.Where(item => item.Id == _ineligibleProviderConfigurationId).ExecuteDeleteAsync();
        await dbContext.PaymentProviders.Where(item => item.Id == _providerId).ExecuteDeleteAsync();
        await dbContext.PaymentProviders.Where(item => item.Id == _ineligibleProviderId).ExecuteDeleteAsync();
        await dbContext.CountryCurrencies.Where(item => item.Id == _countryCurrencyId).ExecuteDeleteAsync();
        await dbContext.Currencies.Where(item => item.Id == _currencyId).ExecuteDeleteAsync();
        await dbContext.Stakeholders.Where(item => item.Id == _stakeholderId).ExecuteDeleteAsync();
        await dbContext.StakeholderTypes.Where(item => item.Id == _stakeholderTypeId).ExecuteDeleteAsync();
        await dbContext.Countries.Where(item => item.Id == _countryId).ExecuteDeleteAsync();

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        await dbContext.SaveChangesAsync();
        await dbContext.Tenants.Where(item => item.Id == _tenantId).ExecuteDeleteAsync();
    }

    private IServiceScope CreateScope() => _factory.Services.CreateScope();
}
