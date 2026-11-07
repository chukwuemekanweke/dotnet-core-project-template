using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasskeyRegistration;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;
using BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyAuthenticationOptions;
using BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyRegistrationOptions;
using BackendProjectTemplate.Application.Authentication.Features.ListPasskeys;
using BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;
using BackendProjectTemplate.Application.Authentication.Features.RenamePasskey;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.Passkeys.Route)]
public sealed class PasskeysController(
    CreatePasskeyRegistrationOptionsHandler createRegistrationOptionsHandler,
    CompletePasskeyRegistrationHandler completeRegistrationHandler,
    CreatePasskeyAuthenticationOptionsHandler createAuthenticationOptionsHandler,
    CompletePasskeySignInHandler completeSignInHandler,
    ListPasskeysHandler listHandler,
    RenamePasskeyHandler renameHandler,
    RemovePasskeyHandler removeHandler,
    IValidator<CreatePasskeyRegistrationOptionsRequest> createOptionsValidator,
    IValidator<CompletePasskeyRequest> completeValidator,
    IValidator<RenamePasskeyRequest> renameValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("registration-options")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<PasskeyOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyOptionsResponse>> CreateRegistrationOptions(
        [FromBody] CreatePasskeyRegistrationOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createOptionsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));

        if (!TryGetAuthenticatedIdentity(out var appUserId, out var stakeholderId, out var tenantId))
            return Unauthorized();

        var result = await createRegistrationOptionsHandler.HandleAsync(
            new CreatePasskeyRegistrationOptionsCommand(appUserId, stakeholderId, tenantId, request.Name),
            cancellationToken);
        return result is null ? Forbid() : Ok(Map(result));
    }

    [HttpPost("registrations")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<PasskeyResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PasskeyResponse>> CompleteRegistration(
        [FromBody] CompletePasskeyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await completeValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));

        if (!TryGetAuthenticatedIdentity(out var appUserId, out var stakeholderId, out var tenantId))
            return Unauthorized();

        var result = await completeRegistrationHandler.HandleAsync(
            new CompletePasskeyRegistrationCommand(
                appUserId, stakeholderId, tenantId, request.FlowToken, request.Credential.GetRawText()),
            cancellationToken);

        return result.Status switch
        {
            PasskeyOperationStatus.Success => StatusCode(StatusCodes.Status201Created, Map(result.Passkey!)),
            PasskeyOperationStatus.ExpiredFlow => Problem(statusCode: StatusCodes.Status410Gone,
                title: "Passkey registration expired", detail: "The passkey registration flow has expired."),
            PasskeyOperationStatus.ConsumedFlow or PasskeyOperationStatus.DuplicateCredential =>
                Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Passkey conflict",
                    Detail = "The passkey flow or credential has already been used." }),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid passkey registration", detail: "The passkey registration could not be validated.")
        };
    }

    [HttpPost("authentication-options")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<PasskeyOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyOptionsResponse>> CreateAuthenticationOptions(CancellationToken cancellationToken)
    {
        if (!currentActor.TenantId.HasValue || currentActor.TenantId.Value == Guid.Empty)
            return BadRequest();

        var result = await createAuthenticationOptionsHandler.HandleAsync(
            new CreatePasskeyAuthenticationOptionsCommand(currentActor.TenantId.Value), cancellationToken);

        return Ok(Map(result));
    }

    [HttpPost("authentication-completions")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<PasskeySignInResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeySignInResponse>> CompleteAuthentication(
        [FromBody] CompletePasskeyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await completeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!currentActor.TenantId.HasValue || currentActor.TenantId.Value == Guid.Empty)
            return BadRequest();
        var result = await completeSignInHandler.HandleAsync(new CompletePasskeySignInCommand(
            currentActor.TenantId.Value,
            request.FlowToken,
            request.Credential.GetRawText(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            ActorContext.FromAnonymousActor(currentActor)), cancellationToken);
        return result.Status switch
        {
            CompletePasskeySignInStatus.Success => Ok(new PasskeySignInResponse(
                AuthenticationOutcomes.Authenticated,
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            CompletePasskeySignInStatus.RequiresTwoFactor => Ok(new PasskeySignInResponse(
                AuthenticationOutcomes.TwoFactorRequired,
                Challenge: result.Challenge!.Token,
                ChallengeExpiresAtUtc: result.Challenge.ExpiresAtUtc)),
            CompletePasskeySignInStatus.ExpiredFlow => Problem(statusCode: StatusCodes.Status410Gone,
                title: "Passkey sign-in expired", detail: "The passkey sign-in flow has expired."),
            CompletePasskeySignInStatus.ConsumedFlow => Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Passkey sign-in consumed", detail: "The passkey sign-in flow has already been used."),
            CompletePasskeySignInStatus.AccountLocked => Problem(statusCode: StatusCodes.Status423Locked,
                title: "Account locked", detail: "The account is currently locked."),
            CompletePasskeySignInStatus.EmailNotVerified => Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Email verification required", detail: "The account email must be verified before signing in."),
            CompletePasskeySignInStatus.TenantMismatch => Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Tenant mismatch", detail: "The passkey does not belong to this tenant."),
            _ => Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid passkey", detail: "The passkey assertion is invalid.")
        };
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<IReadOnlyList<PasskeyResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PasskeyResponse>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedIdentity(out var appUserId, out var stakeholderId, out var tenantId))
            return Unauthorized();
        var result = await listHandler.HandleAsync(
            new ListPasskeysQuery(appUserId, stakeholderId, tenantId), cancellationToken);
        return result is null ? Forbid() : Ok(result.Select(Map).ToArray());
    }

    [HttpPatch("{credentialId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Rename(
        string credentialId,
        [FromBody] RenamePasskeyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await renameValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!TryGetAuthenticatedIdentity(out var appUserId, out var stakeholderId, out var tenantId))
            return Unauthorized();
        var result = await renameHandler.HandleAsync(
            new RenamePasskeyCommand(appUserId, stakeholderId, tenantId, credentialId, request.Name),
            cancellationToken);
        return result switch
        {
            PasskeyOperationStatus.Success => NoContent(),
            PasskeyOperationStatus.NotFound => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to rename passkey")
        };
    }

    [HttpDelete("{credentialId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Remove(string credentialId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedIdentity(out var appUserId, out var stakeholderId, out var tenantId))
            return Unauthorized();
        var result = await removeHandler.HandleAsync(
            new RemovePasskeyCommand(appUserId, stakeholderId, tenantId, credentialId), cancellationToken);
        return result switch
        {
            PasskeyOperationStatus.Success => NoContent(),
            PasskeyOperationStatus.NotFound => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to remove passkey")
        };
    }

    private bool TryGetAuthenticatedIdentity(out Guid appUserId, out Guid stakeholderId, out Guid tenantId)
    {
        var appUserValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var stakeholderValue = User.FindFirstValue(CustomClaimTypes.StakeholderId);
        tenantId = currentActor.TenantId ?? Guid.Empty;
        var hasAppUser = Guid.TryParse(appUserValue, out appUserId);
        var hasStakeholder = Guid.TryParse(stakeholderValue, out stakeholderId);
        return hasAppUser
            && hasStakeholder
            && tenantId != Guid.Empty;
    }

    private static PasskeyOptionsResponse Map(PasskeyOptions options) =>
        new(options.FlowToken, JsonSerializer.Deserialize<JsonElement>(options.OptionsJson), options.ExpiresAtUtc);

    private static PasskeyResponse Map(PasskeyCredential passkey) =>
        new(passkey.Id, passkey.Name, passkey.CreatedAtUtc, passkey.Transports,
            passkey.IsBackupEligible, passkey.IsBackedUp);
}
