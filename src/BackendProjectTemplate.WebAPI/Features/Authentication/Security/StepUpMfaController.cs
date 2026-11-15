using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.AuthenticationSecurity.Route)]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthorizationPolicyNames.RequireActiveSession)]
public sealed class StepUpMfaController(
    VerifyStepUpMfaHandler handler,
    StepUpMfaRequestCanonicalizer canonicalizer,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("step-up/verifications")]
    [EnableRateLimiting(RateLimitingPolicyNames.TwoFactorVerificationPolicy)]
    [StepUpMfaVerificationEndpoint]
    [ProducesResponseType<StepUpMfaVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<StepUpMfaVerificationResponse>> Verify(
        [FromBody] StepUpMfaVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var validationProblem))
            return BadRequest(validationProblem);
        if (!TryGetCurrentSessionId(out var sessionId))
            return Unauthorized();
        if (!canonicalizer.TryCreateFingerprint(
                request.Operation,
                request.IntendedRequest.Method,
                request.IntendedRequest.Path,
                request.IntendedRequest.Query,
                request.IntendedRequest.Body,
                out var fingerprint))
        {
            return BadRequest(AuthenticationProblemDetails.Create(
                StatusCodes.Status400BadRequest,
                AuthenticationErrorCodes.MfaOperationMismatch,
                "Invalid step-up MFA operation",
                "The intended request does not match the registered operation.").Value);
        }

        var result = await handler.HandleAsync(
            new VerifyStepUpMfaCommand(
                TwoFactorVerificationMethod.Authenticator,
                request.Code,
                request.Operation,
                fingerprint,
                sessionId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            VerifyStepUpMfaStatus.Success => Ok(new StepUpMfaVerificationResponse(
                result.AuthorizationToken!,
                result.ExpiresAtUtc!.Value)),
            VerifyStepUpMfaStatus.NotAuthenticated => Unauthorized(),
            VerifyStepUpMfaStatus.EnrollmentRequired => AuthenticationProblemDetails.Create(
                StatusCodes.Status403Forbidden,
                AuthenticationErrorCodes.MfaEnrollmentRequired,
                "MFA enrollment required",
                "Enroll an authenticator before performing privileged operations."),
            VerifyStepUpMfaStatus.AccountLocked => AuthenticationProblemDetails.Create(
                StatusCodes.Status403Forbidden,
                AuthenticationErrorCodes.AccountLocked,
                "Account locked",
                "Too many unsuccessful verification attempts.",
                new Dictionary<string, object?> { ["retryAtUtc"] = result.LockedUntilUtc }),
            VerifyStepUpMfaStatus.InvalidCode or VerifyStepUpMfaStatus.CodeReplayed => AuthenticationProblemDetails.Create(
                StatusCodes.Status401Unauthorized,
                AuthenticationErrorCodes.MfaVerificationFailed,
                "MFA verification failed",
                "The authenticator code is invalid or has already been used."),
            VerifyStepUpMfaStatus.InvalidMethod or VerifyStepUpMfaStatus.InvalidOperation => AuthenticationProblemDetails.Create(
                StatusCodes.Status400BadRequest,
                AuthenticationErrorCodes.MfaVerificationFailed,
                "MFA verification failed",
                "Only authenticator verification for a registered operation is supported."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to complete step-up MFA verification")
        };
    }

    private static bool TryValidateRequest(
        StepUpMfaVerificationRequest? request,
        out ValidationProblemDetails problem)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
            errors["request"] = ["A request body is required."];
        else
        {
            if (request.VerificationMethod != "authenticator")
                errors[nameof(request.VerificationMethod)] = ["Only authenticator verification is supported."];
            if (string.IsNullOrWhiteSpace(request.Code) ||
                string.Concat(request.Code.Where(char.IsDigit)).Length != 6)
                errors[nameof(request.Code)] = ["A six-digit authenticator code is required."];
            if (string.IsNullOrWhiteSpace(request.Operation))
                errors[nameof(request.Operation)] = ["An operation is required."];
            if (request.IntendedRequest is null)
                errors[nameof(request.IntendedRequest)] = ["The intended request is required."];
            else
            {
                if (string.IsNullOrWhiteSpace(request.IntendedRequest.Method))
                    errors["intendedRequest.method"] = ["The HTTP method is required."];
                if (string.IsNullOrWhiteSpace(request.IntendedRequest.Path))
                    errors["intendedRequest.path"] = ["The request path is required."];
            }
        }

        problem = new ValidationProblemDetails(errors);
        return errors.Count == 0;
    }

    private bool TryGetCurrentSessionId(out Guid sessionId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ??
            User.FindFirst(ClaimTypes.Sid)?.Value, out sessionId);
}
