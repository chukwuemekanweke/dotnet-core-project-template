using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;
using BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;
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

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Kyc.AdminKyc;

[Collection(nameof(ContainersCollection))]
public sealed class When_MonitoringAdminKycVerifications_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private readonly string _providerKey = $"kyc-{Guid.CreateVersion7():N}";

    [Fact]
    public async Task AuthorizeFilterPaginateAndReturnSafelyCorrelatedDetail()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.AdminKyc.V1))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminKyc.V1))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await GrantRoleAsync(ApplicationRoles.Administrator);
        var seeded = await SeedKycAsync();
        var query = $"TenantId={TargetTenantId}&StakeholderId={TargetStakeholderId}" +
                    $"&Status=InProgress&ProviderKey={_providerKey}&ProviderStatus=reviewing" +
                    $"&ProviderReference={seeded.CurrentVerificationReference}&Search=grace&Limit=1";
        using var listResponse = await Client.GetAsync($"{EndpointUrl.AdminKyc.V1}?{query}");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await listResponse.Content.ReadAsStringAsync());
        var list = (await listResponse.Content.ReadFromJsonAsync<ListAdminKycVerificationsResult>())!;
        var item = list.Items.ShouldHaveSingleItem();
        item.VerificationId.ShouldBe(seeded.CurrentVerificationId);
        item.LatestProviderStatus.ShouldBe("reviewing");
        item.StakeholderDisplayName.ShouldBe("Grace Hopper");

        using var firstPageResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminKyc.V1}?TenantId={TargetTenantId}&ProviderKey={_providerKey}&Limit=1");
        var firstPage = (await firstPageResponse.Content.ReadFromJsonAsync<ListAdminKycVerificationsResult>())!;
        firstPage.Items.ShouldHaveSingleItem();
        firstPage.NextCursor.ShouldNotBeNull();
        using var secondPageResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminKyc.V1}?TenantId={TargetTenantId}&ProviderKey={_providerKey}&Limit=1" +
            $"&Cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        var secondPage = (await secondPageResponse.Content.ReadFromJsonAsync<ListAdminKycVerificationsResult>())!;
        secondPage.Items.ShouldHaveSingleItem();
        firstPage.Items[0].VerificationId.ShouldNotBe(secondPage.Items[0].VerificationId);

        using var detailResponse = await Client.GetAsync(EndpointUrl.AdminKyc.DetailV1(seeded.CurrentVerificationId));
        var rawDetail = await detailResponse.Content.ReadAsStringAsync();
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK, rawDetail);
        rawDetail.ShouldNotContain("ExternalApplicantId", Case.Insensitive);
        rawDetail.ShouldNotContain("VerificationUrl", Case.Insensitive);
        var detail = await detailResponse.Content.ReadFromJsonAsync<GetAdminKycVerificationDetailResponse>();
        detail.ShouldNotBeNull();
        detail.Provider.LatestProviderStatus.ShouldBe("reviewing");
        detail.CallbackHistory.ShouldHaveSingleItem().RequestId.ShouldBe(seeded.CorrelatedRequestId);

        using var invalidStatus = await Client.GetAsync($"{EndpointUrl.AdminKyc.V1}?Status=Approved");
        invalidStatus.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var invalidCursor = await Client.GetAsync($"{EndpointUrl.AdminKyc.V1}?Cursor=not-base64");
        invalidCursor.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var missing = await Client.GetAsync(EndpointUrl.AdminKyc.DetailV1(Guid.CreateVersion7()));
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.KycWebhookInboxes.IgnoreQueryFilters()
                .Where(item => item.ProviderKey == _providerKey)
                .ExecuteDeleteAsync();
            await dbContext.KycVerifications.IgnoreQueryFilters()
                .Where(item => item.ProviderKey == _providerKey)
                .ExecuteDeleteAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<SeededKyc> SeedKycAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = UtcNow();

        var prior = KycVerification.Start(
            TargetTenantId,
            TargetStakeholderId,
            _providerKey,
            "applicant-shared",
            "applicant-reference",
            now.AddMinutes(-10));
        prior.MarkSessionRequested(now.AddMinutes(-10));
        prior.AttachSession(new KycSessionResult(
            "verification-prior",
            "session-prior",
            new Uri("https://example.com/kyc/prior"),
            null));
        prior.ApplyProviderResult(KycVerificationTransition.Rejected, "applicant-reference", now.AddMinutes(-9));

        var current = KycVerification.Start(
            TargetTenantId,
            TargetStakeholderId,
            _providerKey,
            "applicant-shared",
            "applicant-reference",
            now);
        current.MarkSessionRequested(now);
        current.AttachSession(new KycSessionResult(
            "verification-current",
            "session-current",
            new Uri("https://example.com/kyc/current"),
            null));
        current.ApplyProviderResult(KycVerificationTransition.InProgress, "applicant-reference", now);
        dbContext.KycVerifications.AddRange(prior, current);

        var correlatedRequestId = $"request-{Guid.CreateVersion7():N}";
        var correlated = KycWebhookInbox.Create(
            _providerKey,
            correlatedRequestId,
            "VERIFICATION_STATUS_CHANGED",
            "verification-current",
            "session-current",
            "applicant-reference",
            "applicant-shared",
            "reviewing",
            null,
            now.AddMinutes(1));
        correlated.MarkProcessed(KycWebhookProcessingOutcome.NoStateTransition, now.AddMinutes(1));
        var ambiguous = KycWebhookInbox.Create(
            _providerKey,
            $"request-{Guid.CreateVersion7():N}",
            "VERIFICATION_STATUS_CHANGED",
            "verification-current",
            "session-prior",
            "applicant-reference",
            "applicant-shared",
            "must-not-appear",
            false,
            now.AddMinutes(2));
        var applicantOnly = KycWebhookInbox.Create(
            _providerKey,
            $"request-{Guid.CreateVersion7():N}",
            "VERIFICATION_STATUS_CHANGED",
            null,
            null,
            "applicant-reference",
            "applicant-shared",
            "must-not-appear",
            false,
            now.AddMinutes(3));
        dbContext.KycWebhookInboxes.AddRange(correlated, ambiguous, applicantOnly);
        await dbContext.SaveChangesAsync();
        return new SeededKyc(current.Id, "verification-current", correlatedRequestId);
    }

    private sealed record SeededKyc(
        Guid CurrentVerificationId,
        string CurrentVerificationReference,
        string CorrelatedRequestId);
}
