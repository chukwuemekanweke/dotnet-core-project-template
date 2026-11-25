using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.AdminKyc;

public sealed class When_GettingAdminKycSummary_WithNoVerifications_Should
{
    [Fact]
    public async Task ReturnZeroCountsWithoutFilters()
    {
        var repository = Substitute.For<IKycAdministrationReadModelRepository>();
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero));
        AdminKycSummaryFilter? captured = null;
        repository.GetSummaryAsync(
                Arg.Do<AdminKycSummaryFilter>(filter => captured = filter),
                Arg.Any<CancellationToken>())
            .Returns(new AdminKycSummaryReadModel(0, 0, 0));

        var result = await new GetAdminKycSummaryHandler(repository, clock).HandleAsync(
            new GetAdminKycSummaryCommand(null, "  "),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.TenantId.ShouldBeNull();
        captured.ProviderKey.ShouldBeNull();
        result.PendingCount.ShouldBe(0);
        result.InProgressCount.ShouldBe(0);
        result.RejectedLastSevenDaysCount.ShouldBe(0);
    }
}
