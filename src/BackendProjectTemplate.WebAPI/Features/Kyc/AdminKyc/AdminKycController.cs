using Asp.Versioning;
using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;
using BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;
using BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminKyc.Route)]
[Authorize(Policy = PermissionPolicyNames.KycRead)]
public sealed class AdminKycController(
    ListAdminKycVerificationsValidator listValidator,
    ListAdminKycVerificationsHandler listHandler,
    GetAdminKycVerificationDetailHandler detailHandler,
    GetAdminKycSummaryValidator summaryValidator,
    GetAdminKycSummaryHandler summaryHandler) : ControllerBase
{
    /// <summary>
    /// Summarizes KYC attempts: current Pending and InProgress counts, and attempts rejected in the
    /// rolling seven-day UTC window [asOfUtc - 7 days, asOfUtc). Counts are attempts, not stakeholders.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<GetAdminKycSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetAdminKycSummaryResponse>> GetSummary(
        [FromQuery] GetAdminKycSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await summaryValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await summaryHandler.HandleAsync(
            new GetAdminKycSummaryCommand(request.TenantId, request.ProviderKey),
            cancellationToken));
    }

    /// <summary>Lists KYC verification attempts for administrative monitoring.</summary>
    [HttpGet]
    [ProducesResponseType<ListAdminKycVerificationsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListAdminKycVerificationsResult>> List(
        [FromQuery] ListAdminKycVerificationsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await listHandler.HandleAsync(
            new ListAdminKycVerificationsCommand(
                request.TenantId,
                request.StakeholderId,
                StrictEnumParser.ParseOptional<KycVerificationStatus>(request.Status),
                request.ProviderKey,
                request.ProviderStatus,
                request.ProviderReference,
                request.Search,
                request.StartedFromUtc,
                request.StartedToUtc,
                request.Cursor,
                request.Limit),
            cancellationToken));
    }

    /// <summary>Gets one KYC verification attempt and its safely correlated callback history.</summary>
    [HttpGet("{verificationId:guid}")]
    [ProducesResponseType<GetAdminKycVerificationDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAdminKycVerificationDetailResponse>> Get(
        [FromRoute] Guid verificationId,
        CancellationToken cancellationToken)
    {
        if (verificationId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(verificationId)] = ["Verification id must not be empty."]
                }));
        }

        var result = await detailHandler.HandleAsync(
            new GetAdminKycVerificationDetailCommand(verificationId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
