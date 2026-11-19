using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.AdminKyc;

public sealed class When_ListingAdminKycVerifications_Should
{
    [Fact]
    public async Task NormalizeFiltersAndReturnDeterministicCursor()
    {
        var repository = Substitute.For<IKycAdministrationReadModelRepository>();
        var now = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
        var item = new AdminKycVerificationListItemReadModel(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Grace Hopper",
            "kycaid",
            KycVerificationStatus.InProgress,
            "reviewing",
            "verification-1",
            now,
            now,
            null,
            null);
        AdminKycVerificationListRequest? captured = null;
        repository.ListVerificationsAsync(
                Arg.Do<AdminKycVerificationListRequest>(request => captured = request),
                Arg.Any<CancellationToken>())
            .Returns(new AdminKycVerificationListPage([item], true));
        var cursor = CursorPagination.Encode(now.AddMinutes(1), Guid.CreateVersion7());

        var result = await new ListAdminKycVerificationsHandler(repository).HandleAsync(
            new ListAdminKycVerificationsCommand(
                item.TenantId,
                item.StakeholderId,
                KycVerificationStatus.InProgress,
                " kycaid ",
                " reviewing ",
                " verification-1 ",
                " grace ",
                now.AddDays(-1),
                now.AddDays(1),
                cursor,
                1),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.ProviderKey.ShouldBe("kycaid");
        captured.ProviderStatus.ShouldBe("reviewing");
        captured.ProviderReference.ShouldBe("verification-1");
        captured.Search.ShouldBe("grace");
        result.Items.ShouldHaveSingleItem().Status.ShouldBe("InProgress");
        CursorPagination.Decode(result.NextCursor).EntityId.ShouldBe(item.VerificationId);
    }
}
