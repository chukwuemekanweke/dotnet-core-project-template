using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.AdminKyc;

public sealed class When_GettingAdminKycVerificationDetail_Should
{
    [Fact]
    public async Task BoundCallbackHistoryAndMapDistinctStatuses()
    {
        var repository = Substitute.For<IKycAdministrationReadModelRepository>();
        var verificationId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
        repository.GetVerificationDetailAsync(verificationId, 50, Arg.Any<CancellationToken>())
            .Returns(new AdminKycVerificationDetailReadModel(
                new AdminKycVerificationReadModel(
                    verificationId,
                    tenantId,
                    stakeholderId,
                    "kycaid",
                    KycVerificationStatus.InProgress,
                    KycVerificationTransition.InProgress,
                    now,
                    now,
                    null,
                    now),
                new AdminKycStakeholderReadModel(stakeholderId, "Grace Hopper", false),
                new AdminKycProviderReadModel(
                    "kycaid",
                    "applicant-1",
                    "verification-1",
                    "session-1",
                    "reviewing",
                    now),
                [new AdminKycCallbackReadModel(
                    "request-1",
                    "VERIFICATION_STATUS_CHANGED",
                    "reviewing",
                    null,
                    KycWebhookProcessingOutcome.NoStateTransition,
                    now,
                    now)]));

        var result = await new GetAdminKycVerificationDetailHandler(repository).HandleAsync(
            new GetAdminKycVerificationDetailCommand(verificationId),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Verification.Status.ShouldBe("InProgress");
        result.Provider.LatestProviderStatus.ShouldBe("reviewing");
        result.CallbackHistory.ShouldHaveSingleItem().ProcessingOutcome.ShouldBe("NoStateTransition");
        await repository.Received(1).GetVerificationDetailAsync(
            verificationId,
            50,
            Arg.Any<CancellationToken>());
    }
}
