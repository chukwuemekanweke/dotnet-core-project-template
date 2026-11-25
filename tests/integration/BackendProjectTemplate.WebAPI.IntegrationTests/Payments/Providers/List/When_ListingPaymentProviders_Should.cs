using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Payments.Providers;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Payments.Providers.List;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingPaymentProviders_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private readonly string _prefix = $"IT Provider {Guid.CreateVersion7():N}";
    private readonly List<Guid> _providerIds = [];

    [Fact]
    public async Task ReturnEveryProviderInDeterministicOrderUnderReadPermission()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.PaymentProviders.V1))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(EndpointUrl.PaymentProviders.V1))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await GrantRoleAsync(ApplicationRoles.ComplianceOfficer);
        using (var withoutPaymentsRead = await Client.GetAsync(EndpointUrl.PaymentProviders.V1))
        {
            withoutPaymentsRead.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await RevokeRoleAsync(ApplicationRoles.ComplianceOfficer);
        await GrantRoleAsync(ApplicationRoles.FinanceOfficer);
        var seeded = await SeedProvidersAsync();

        using var response = await Client.GetAsync(EndpointUrl.PaymentProviders.V1);
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        using (var document = JsonDocument.Parse(raw))
        {
            document.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                // Only the four public fields: no configurations, credentials or navigation properties.
                item.EnumerateObject().Select(property => property.Name)
                    .ShouldBe(["id", "providerName", "providerKey", "isActive"], ignoreOrder: true);
            }
        }

        var providers = (await response.Content.ReadFromJsonAsync<List<PaymentProviderResponse>>())!;
        var ours = providers.Where(item => item.ProviderName.StartsWith(_prefix, StringComparison.Ordinal)).ToList();
        ours.Select(item => item.Id).ShouldBe(seeded.ExpectedOrder);
        ours.ShouldContain(new PaymentProviderResponse(
            seeded.Inactive.Id,
            seeded.Inactive.ProviderName,
            seeded.Inactive.ProviderKey,
            false));
        ours.Count(item => item.IsActive).ShouldBe(3);

        // Activation still requires payments.manage plus a step-up proof: a raw request without one is refused.
        using (var rawClient = CreateRawClient())
        {
            rawClient.DefaultRequestHeaders.Add("X-Tenant-Id", AdminTenantId.ToString());
            rawClient.DefaultRequestHeaders.Authorization = Client.DefaultRequestHeaders.Authorization;
            using var withoutProof = await rawClient.PutAsJsonAsync(
                $"{EndpointUrl.PaymentProviders.V1}/{seeded.Inactive.Id}/activation",
                new SetPaymentProviderActivationRequest(true));
            withoutProof.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            using var problem = JsonDocument.Parse(await withoutProof.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("code").GetString().ShouldBe("mfa_verification_required");
        }

        using (var activation = await Client.PutAsJsonAsync(
                   $"{EndpointUrl.PaymentProviders.V1}/{seeded.Inactive.Id}/activation",
                   new SetPaymentProviderActivationRequest(true)))
        {
            activation.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        var afterActivation = (await Client.GetFromJsonAsync<List<PaymentProviderResponse>>(
            EndpointUrl.PaymentProviders.V1))!;
        afterActivation.Single(item => item.Id == seeded.Inactive.Id).IsActive.ShouldBeTrue();
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PaymentProviderConfigurations.IgnoreQueryFilters()
                .Where(item => _providerIds.Contains(item.PaymentProviderId))
                .ExecuteDeleteAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<SeededProviders> SeedProvidersAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gamma = PaymentProvider.Create($"{_prefix} Gamma", Key(), true);
        var inactive = PaymentProvider.Create($"{_prefix} Beta", Key(), false);
        var alphaFirst = PaymentProvider.Create($"{_prefix} Alpha", Key(), true);
        var alphaSecond = PaymentProvider.Create($"{_prefix} Alpha", Key(), true);

        // A configuration exists but must never be projected into the listing.
        gamma.SetConfiguration(CurrencyId, PaymentIntent.WalletTopUp, PaymentMethodType.PaymentLink, true);
        dbContext.PaymentProviders.AddRange(gamma, inactive, alphaFirst, alphaSecond);
        await dbContext.SaveChangesAsync();
        foreach (var provider in new[] { gamma, inactive, alphaFirst, alphaSecond })
        {
            _providerIds.Add(provider.Id);
            TrackPaymentProviderForCleanup(provider.Id);
        }

        // Equal names fall back to Id. PostgreSQL compares uuid bytewise, which matches ordinal
        // ordering of the canonical lowercase text form.
        var alphas = new[] { alphaFirst, alphaSecond }
            .OrderBy(provider => provider.Id.ToString(), StringComparer.Ordinal)
            .Select(provider => provider.Id);
        return new SeededProviders(inactive, [.. alphas, inactive.Id, gamma.Id]);
    }

    private static string Key() => $"it-{Guid.CreateVersion7():N}";

    private sealed record SeededProviders(PaymentProvider Inactive, IReadOnlyList<Guid> ExpectedOrder);
}
