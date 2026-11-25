using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.AdminKyc;

public sealed class When_GettingAdminKycSummary_WithFilters_Should
{
    [Fact]
    public async Task QueryHalfOpenSevenDayWindowFromSingleClockReading()
    {
        var repository = Substitute.For<IKycAdministrationReadModelRepository>();
        var clock = Substitute.For<TimeProvider>();
        var asOfUtc = new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero);
        clock.GetUtcNow().Returns(asOfUtc, asOfUtc.AddMinutes(5));
        var tenantId = Guid.CreateVersion7();
        AdminKycSummaryFilter? captured = null;
        repository.GetSummaryAsync(
                Arg.Do<AdminKycSummaryFilter>(filter => captured = filter),
                Arg.Any<CancellationToken>())
            .Returns(new AdminKycSummaryReadModel(14, 38, 9));

        var result = await new GetAdminKycSummaryHandler(repository, clock).HandleAsync(
            new GetAdminKycSummaryCommand(tenantId, " kycaid "),
            CancellationToken.None);

        clock.Received(1).GetUtcNow();
        captured.ShouldNotBeNull();
        captured.TenantId.ShouldBe(tenantId);
        captured.ProviderKey.ShouldBe("kycaid");
        captured.RejectedToUtc.ShouldBe(asOfUtc);
        captured.RejectedFromUtc.ShouldBe(new DateTimeOffset(2026, 11, 18, 19, 0, 0, TimeSpan.Zero));
        result.ShouldBe(new GetAdminKycSummaryResponse(
            14,
            38,
            9,
            asOfUtc,
            new DateTimeOffset(2026, 11, 18, 19, 0, 0, TimeSpan.Zero)));
    }
}
