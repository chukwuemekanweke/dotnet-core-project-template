using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.DisableTwoFactor;
using BackendProjectTemplate.Application.Authentication.Features.GetTwoFactorStatus;
using BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;
using BackendProjectTemplate.Application.Authentication.Features.SetupTwoFactor;
using BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.AuthenticationSecurity.Route)]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthorizationPolicyNames.RequireActiveSession)]
public sealed class TwoFactorController(
    GetTwoFactorStatusHandler statusHandler,
    SetupTwoFactorHandler setupHandler,
    VerifyTwoFactorEnrollmentHandler enrollmentHandler,
    RegenerateTwoFactorRecoveryCodesHandler recoveryCodesHandler,
    DisableTwoFactorHandler disableHandler,
    IValidator<TwoFactorEnrollmentRequest> enrollmentValidator,
    IValidator<TwoFactorProofRequest> proofValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("two-factor")]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await statusHandler.HandleAsync(
            new GetTwoFactorStatusQuery(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status == GetTwoFactorStatusStatus.Success
            ? Ok(new TwoFactorStatusResponse(result.Enabled, result.RecoveryCodesRemaining))
            : Unauthorized();
    }

    [HttpPost("two-factor/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup(CancellationToken cancellationToken)
    {
        var result = await setupHandler.HandleAsync(
            new SetupTwoFactorCommand(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            SetupTwoFactorStatus.Success => Ok(new TwoFactorSetupResponse(result.SharedKey!, result.AuthenticatorUri!)),
            SetupTwoFactorStatus.AlreadyEnabled => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Two-factor authentication already enabled"
            }),
            SetupTwoFactorStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to set up two-factor authentication")
        };
    }

    [HttpPost("two-factor/verify")]
    public async Task<ActionResult<RecoveryCodesResponse>> VerifyEnrollment(
        [FromBody] TwoFactorEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await enrollmentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));

        var result = await enrollmentHandler.HandleAsync(
            new VerifyTwoFactorEnrollmentCommand(request.Code, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            VerifyTwoFactorEnrollmentStatus.Success => Ok(new RecoveryCodesResponse(result.RecoveryCodes!)),
            VerifyTwoFactorEnrollmentStatus.InvalidCode => Unauthorized(),
            VerifyTwoFactorEnrollmentStatus.AlreadyEnabled => Conflict(),
            VerifyTwoFactorEnrollmentStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to enable two-factor authentication")
        };
    }

    [HttpPost("two-factor/recovery-codes")]
    public async Task<ActionResult<RecoveryCodesResponse>> RegenerateRecoveryCodes(
        [FromBody] TwoFactorProofRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await proofValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        var result = await recoveryCodesHandler.HandleAsync(
            new RegenerateTwoFactorRecoveryCodesCommand(
                ParseMethod(request.VerificationMethod),
                request.Code,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            RegenerateTwoFactorRecoveryCodesStatus.Success => Ok(new RecoveryCodesResponse(result.RecoveryCodes!)),
            RegenerateTwoFactorRecoveryCodesStatus.InvalidProof => Unauthorized(),
            RegenerateTwoFactorRecoveryCodesStatus.NotEnabled => Conflict(),
            RegenerateTwoFactorRecoveryCodesStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to regenerate recovery codes")
        };
    }

    [HttpPost("two-factor/disable")]
    public async Task<IActionResult> Disable(
        [FromBody] TwoFactorProofRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await proofValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        var result = await disableHandler.HandleAsync(
            new DisableTwoFactorCommand(
                ParseMethod(request.VerificationMethod),
                request.Code,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result switch
        {
            DisableTwoFactorResult.Success => NoContent(),
            DisableTwoFactorResult.InvalidProof => Unauthorized(),
            DisableTwoFactorResult.NotEnabled => Conflict(),
            DisableTwoFactorResult.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to disable two-factor authentication")
        };
    }

    private static TwoFactorVerificationMethod ParseMethod(string method) =>
        method == "authenticator"
            ? TwoFactorVerificationMethod.Authenticator
            : TwoFactorVerificationMethod.RecoveryCode;
}
