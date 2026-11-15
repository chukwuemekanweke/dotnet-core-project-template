using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireStepUpMfaAttribute(string operation) : Attribute, IAsyncResourceFilter, IOrderedFilter
{
    public const string HeaderName = "X-Step-Up-Authorization";
    public int Order => int.MinValue + 100;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var requestServices = context.HttpContext.RequestServices;
        var identityService = requestServices.GetRequiredService<IAuthenticationIdentityService>();
        var proofStore = requestServices.GetRequiredService<IStepUpMfaProofStore>();
        var canonicalizer = requestServices.GetRequiredService<StepUpMfaRequestCanonicalizer>();
        var telemetry = requestServices.GetRequiredService<ICustomTelemetryContext>();
        var logger = requestServices.GetRequiredService<ILogger<RequireStepUpMfaAttribute>>();
        var user = context.HttpContext.User;

        if (!TryResolveClaims(user, out var appUserId, out var stakeholderId, out var sessionId))
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaProofInvalid,
                "Step-up MFA authorization invalid", "The step-up authorization could not be validated.");
            return;
        }

        var appUser = await identityService.FindByIdAsync(appUserId);
        if (appUser is null || appUser.IsDeleted)
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaProofInvalid,
                "Step-up MFA authorization invalid", "The step-up authorization could not be validated.");
            return;
        }
        if (!await identityService.GetTwoFactorEnabledAsync(appUser))
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaEnrollmentRequired,
                "MFA enrollment required", "Enroll an authenticator before performing this operation.");
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var header) || header.Count != 1)
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaVerificationRequired,
                "Step-up MFA verification required", "A fresh step-up MFA authorization is required.");
            return;
        }

        string? fingerprint;
        try
        {
            fingerprint = await canonicalizer.CreateFingerprintAsync(
                operation,
                context.HttpContext.Request,
                context.HttpContext.RequestAborted);
        }
        catch (JsonException)
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaProofInvalid,
                "Step-up MFA authorization invalid", "The protected request could not be fingerprinted.");
            return;
        }

        if (fingerprint is null)
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaOperationMismatch,
                "Step-up MFA operation mismatch", "The proof does not match this operation.");
            return;
        }

        StepUpMfaProofConsumptionStatus status;
        try
        {
            status = await proofStore.ConsumeAsync(
                header[0]!,
                new StepUpMfaProofConsumption(
                    appUserId,
                    stakeholderId,
                    sessionId,
                    await identityService.GetSecurityStampAsync(appUser),
                    operation,
                    fingerprint),
                context.HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Step-up MFA proof store failed closed for operation {Operation}", operation);
            telemetry.AddCustomEvent(Observability.EventNames.Authentication.StepUpMfaProofStoreFailed,
                AdditionalProperties(stakeholderId, sessionId, operation));
            context.Result = Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaProofInvalid,
                "Step-up MFA authorization unavailable", "The step-up authorization could not be validated.");
            return;
        }

        if (status != StepUpMfaProofConsumptionStatus.Consumed)
        {
            context.Result = status switch
            {
                StepUpMfaProofConsumptionStatus.Expired => Problem(StatusCodes.Status403Forbidden,
                    AuthenticationErrorCodes.MfaProofExpired, "Step-up MFA proof expired", "Verify MFA again and retry."),
                StepUpMfaProofConsumptionStatus.AlreadyConsumed => Problem(StatusCodes.Status403Forbidden,
                    AuthenticationErrorCodes.MfaProofConsumed, "Step-up MFA proof already consumed", "Verify MFA again and retry."),
                StepUpMfaProofConsumptionStatus.OperationMismatch or StepUpMfaProofConsumptionStatus.RequestMismatch =>
                    Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaOperationMismatch,
                        "Step-up MFA operation mismatch", "The proof does not match this request."),
                _ => Problem(StatusCodes.Status403Forbidden, AuthenticationErrorCodes.MfaProofInvalid,
                    "Step-up MFA proof invalid", "The step-up authorization could not be validated.")
            };
            return;
        }

        telemetry.AddCustomEvent(Observability.EventNames.Authentication.StepUpMfaProofConsumed,
            AdditionalProperties(stakeholderId, sessionId, operation));
        await next();
    }

    private static bool TryResolveClaims(ClaimsPrincipal principal, out Guid appUserId, out Guid stakeholderId, out Guid sessionId)
    {
        appUserId = default;
        stakeholderId = default;
        sessionId = default;
        return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out appUserId) &&
            Guid.TryParse(principal.FindFirst(CustomClaimTypes.StakeholderId)?.Value, out stakeholderId) &&
            Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ??
                principal.FindFirst(ClaimTypes.Sid)?.Value, out sessionId);
    }

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);

    private static Dictionary<string, string> AdditionalProperties(Guid stakeholderId, Guid sessionId, string operation) =>
        new()
        {
            [Observability.PropertyNames.Common.StakeholderId] = stakeholderId.ToString(),
            [Observability.PropertyNames.Authentication.SessionId] = sessionId.ToString(),
            [Observability.PropertyNames.Authentication.StepUpOperation] = operation
        };
}
