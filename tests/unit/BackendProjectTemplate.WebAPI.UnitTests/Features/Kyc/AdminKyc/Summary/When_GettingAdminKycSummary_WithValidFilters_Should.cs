using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.AdminKyc.Summary;

public sealed class When_GettingAdminKycSummary_WithValidFilters_Should
{
    [Fact]
    public async Task ReturnOkWithSummaryAndReportingWindow()
    {
        var context = new AdminKycSummaryControllerTestContext();
        var asOfUtc = new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero);
        var tenantId = Guid.CreateVersion7();
        context.Clock.GetUtcNow().Returns(asOfUtc);
        context.Repository
            .GetSummaryAsync(
                Arg.Is<AdminKycSummaryFilter>(filter =>
                    filter.TenantId == tenantId && filter.ProviderKey == "kycaid"),
                Arg.Any<CancellationToken>())
            .Returns(new AdminKycSummaryReadModel(14, 38, 9));

        var result = await context.CreateController().GetSummary(
            new GetAdminKycSummaryRequest(tenantId, "kycaid"),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(new GetAdminKycSummaryResponse(14, 38, 9, asOfUtc, asOfUtc.AddDays(-7)));
    }
}
