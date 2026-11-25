using BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.AdminKyc.Summary;

public sealed class When_GettingAdminKycSummary_WithInvalidFilters_Should
{
    public static TheoryData<Guid?, string?, string> InvalidFilters => new()
    {
        { Guid.Empty, null, nameof(GetAdminKycSummaryRequest.TenantId) },
        { null, new string('k', 51), nameof(GetAdminKycSummaryRequest.ProviderKey) }
    };

    [Theory]
    [MemberData(nameof(InvalidFilters))]
    public async Task ReturnBadRequestWithoutQuerying(Guid? tenantId, string? providerKey, string invalidField)
    {
        var context = new AdminKycSummaryControllerTestContext();

        var result = await context.CreateController().GetSummary(
            new GetAdminKycSummaryRequest(tenantId, providerKey),
            CancellationToken.None);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ValidationProblemDetails>().Errors.Keys.ShouldContain(invalidField);
        await context.Repository.DidNotReceiveWithAnyArgs()
            .GetSummaryAsync(default!, default);
    }
}
