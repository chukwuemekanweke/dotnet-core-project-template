using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Kyc.AdminKyc.Summary;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingAdminKycSummary_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private readonly string _providerKey = $"kyc-{Guid.CreateVersion7():N}";

    [Fact]
    public async Task AuthorizeValidateAndReturnFilteredCounts()
    {
        var url = $"{EndpointUrl.AdminKyc.SummaryV1}?ProviderKey={_providerKey}";
        using (var anonymous = await Client.GetAsync(url))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(url))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        // ComplianceOfficer holds kyc.read but no administrator-only permissions.
        await GrantRoleAsync(ApplicationRoles.ComplianceOfficer);
        await SeedKycAsync();

        using (var emptyTenant = await Client.GetAsync($"{EndpointUrl.AdminKyc.SummaryV1}?TenantId={Guid.Empty}"))
        {
            emptyTenant.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var malformedTenant = await Client.GetAsync($"{EndpointUrl.AdminKyc.SummaryV1}?TenantId=not-a-guid"))
        {
            malformedTenant.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var longProvider = await Client.GetAsync(
                   $"{EndpointUrl.AdminKyc.SummaryV1}?ProviderKey={new string('k', 51)}"))
        {
            longProvider.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var providerResponse = await Client.GetAsync(url);
        var raw = await providerResponse.Content.ReadAsStringAsync();
        providerResponse.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        raw.ShouldContain("\"rejectedLastSevenDaysCount\"");
        var providerSummary = (await providerResponse.Content.ReadFromJsonAsync<GetAdminKycSummaryResponse>())!;
        providerSummary.PendingCount.ShouldBe(1);
        providerSummary.InProgressCount.ShouldBe(1);
        providerSummary.RejectedLastSevenDaysCount.ShouldBe(2);
        (providerSummary.AsOfUtc - providerSummary.RejectedFromUtc).ShouldBe(TimeSpan.FromDays(7));
        providerSummary.AsOfUtc.Offset.ShouldBe(TimeSpan.Zero);

        using var combinedResponse = await Client.GetAsync($"{url}&TenantId={TargetTenantId}");
        var combined = (await combinedResponse.Content.ReadFromJsonAsync<GetAdminKycSummaryResponse>())!;
        combined.PendingCount.ShouldBe(1);
        combined.InProgressCount.ShouldBe(0);
        combined.RejectedLastSevenDaysCount.ShouldBe(1);

        using var emptyResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminKyc.SummaryV1}?ProviderKey=kyc-{Guid.CreateVersion7():N}");
        var empty = (await emptyResponse.Content.ReadFromJsonAsync<GetAdminKycSummaryResponse>())!;
        empty.PendingCount.ShouldBe(0);
        empty.InProgressCount.ShouldBe(0);
        empty.RejectedLastSevenDaysCount.ShouldBe(0);
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.KycVerifications.IgnoreQueryFilters()
                .Where(item => item.ProviderKey == _providerKey)
                .ExecuteDeleteAsync();
        }

        await base.DisposeAsync();
    }

    private async Task SeedKycAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = UtcNow();

        var pending = KycVerification.Start(
            TargetTenantId, TargetStakeholderId, _providerKey, "applicant-target", null, now.AddHours(-1));
        var inProgress = KycVerification.Start(
            ForeignTenantId, ForeignStakeholderId, _providerKey, "applicant-foreign", null, now.AddHours(-1));
        inProgress.ApplyProviderResult(KycVerificationTransition.InProgress, null, now.AddMinutes(-50));
        var recentlyRejected = KycVerification.Start(
            TargetTenantId, TargetStakeholderId, _providerKey, "applicant-target", null, now.AddDays(-20));
        recentlyRejected.ApplyProviderResult(KycVerificationTransition.Rejected, null, now.AddDays(-1));
        var foreignRejected = KycVerification.Start(
            ForeignTenantId, ForeignStakeholderId, _providerKey, "applicant-foreign", null, now.AddDays(-3));
        foreignRejected.ApplyProviderResult(KycVerificationTransition.Rejected, null, now.AddDays(-2));
        var staleRejected = KycVerification.Start(
            TargetTenantId, TargetStakeholderId, _providerKey, "applicant-target", null, now.AddDays(-9));
        staleRejected.ApplyProviderResult(KycVerificationTransition.Rejected, null, now.AddDays(-8));
        recentlyRejected.Status.ShouldBe(KycVerificationStatus.Rejected);

        dbContext.KycVerifications.AddRange(pending, inProgress, recentlyRejected, foreignRejected, staleRejected);
        await dbContext.SaveChangesAsync();
    }
}
