using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;
using BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.AdminKyc.Summary;

internal sealed class AdminKycSummaryControllerTestContext
{
    public IKycAdministrationReadModelRepository Repository { get; } =
        Substitute.For<IKycAdministrationReadModelRepository>();

    public TimeProvider Clock { get; } = Substitute.For<TimeProvider>();

    public AdminKycController CreateController() =>
        new(
            new ListAdminKycVerificationsValidator(),
            new ListAdminKycVerificationsHandler(Repository),
            new GetAdminKycVerificationDetailHandler(Repository),
            new GetAdminKycSummaryValidator(),
            new GetAdminKycSummaryHandler(Repository, Clock));
}
