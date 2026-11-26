using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Shouldly;
using System.Net;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Auditing.AdminAuditLogs.Export;

[Collection(nameof(ContainersCollection))]
public sealed class When_ExportingAdministrativeAuditLogs_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ExportFilteredRecordsAsEscapedCsv()
    {
        await AuthenticateAsync();
        using (var anonymousClient = CreateRawClient())
        {
            anonymousClient.DefaultRequestHeaders.Add("X-Tenant-Id", AdminTenantId.ToString());
            using var anonymous = await anonymousClient.GetAsync(EndpointUrl.AdminAuditLogs.ExportV1);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await GrantRoleAsync(ApplicationRoles.FinanceOfficer);
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminAuditLogs.ExportV1))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        // FinanceOfficer may allocate and cancel subscriptions but cannot read or export their audit history.
        var tenantAllocation = await AllocateSuccessfullyAsync(TenantAllocation() with { Reason = "=HYPERLINK(\"https://evil.test\",\"x\"), \"quoted\"" });
        var stakeholderAllocation = await AllocateSuccessfullyAsync(StakeholderAllocation(TargetStakeholderId));
        await RevokeRoleAsync(ApplicationRoles.FinanceOfficer);
        await GrantRoleAsync(ApplicationRoles.Administrator);

        using var response = await Client.GetAsync(
            $"{EndpointUrl.AdminAuditLogs.ExportV1}?actorUserId={AdminUserId}&tenantId={TargetTenantId}&action={AdministrativeAuditActions.Subscriptions.Allocated}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        response.Content.Headers.ContentDisposition!.FileName!.ShouldContain("administrative-audit-");
        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines[0].ShouldBe(string.Join(',', AdministrativeAuditCsvWriter.Headers));
        lines.Length.ShouldBe(3);
        lines[1].ShouldStartWith($"{FindAuditId(lines, stakeholderAllocation.SubscriptionId)},");
        csv.ShouldContain($",{tenantAllocation.SubscriptionId},");
        csv.ShouldContain("\"'=HYPERLINK(\"\"https://evil.test\"\",\"\"x\"\"), \"\"quoted\"\"\"");
        csv.ShouldNotContain(",=HYPERLINK");
        csv.ShouldContain($",{TargetTenantId},");
        csv.ShouldNotContain("@");

        using (var empty = await Client.GetAsync($"{EndpointUrl.AdminAuditLogs.ExportV1}?actorUserId={AdminUserId}&tenantId={ForeignTenantId}"))
        {
            empty.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await empty.Content.ReadAsStringAsync()).ShouldBe(string.Join(',', AdministrativeAuditCsvWriter.Headers) + "\r\n");
        }

        using var invalid = await Client.GetAsync($"{EndpointUrl.AdminAuditLogs.ExportV1}?fromUtc=2026-11-02T00:00:00Z&toUtc=2026-11-01T00:00:00Z");
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string FindAuditId(string[] lines, Guid subscriptionId) =>
        lines.Single(line => line.Contains($",{subscriptionId},", StringComparison.Ordinal)).Split(',')[0];
}
