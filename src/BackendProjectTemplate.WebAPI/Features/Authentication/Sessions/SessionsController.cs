using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.ListSessions;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RevokeOtherSessions;
using BackendProjectTemplate.Application.Authentication.Features.RevokeSession;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Formatting;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.Sessions.Route)]
public sealed class SessionsController(
    SignInHandler handler,
    GoogleSignInHandler googleSignInHandler,
    LogoutSessionHandler logoutSessionHandler,
    RefreshSessionHandler refreshSessionHandler,
    IValidator<SignInRequest> validator,
    IValidator<GoogleSignInRequest> googleSignInValidator,
    IValidator<RefreshSessionRequest> refreshSessionValidator,
    TimeProvider timeProvider,
    ICurrentActor currentActor,
    ListSessionsHandler listSessionsHandler,
    RevokeSessionHandler revokeSessionHandler,
    RevokeOtherSessionsHandler revokeOtherSessionsHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<IReadOnlyList<ActiveSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActiveSessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        if (!TryGetSessionIdentity(out var stakeholderId, out var currentSessionId))
            return Unauthorized();

        var sessions = await listSessionsHandler.HandleAsync(
            new ListSessionsQuery(stakeholderId, currentSessionId), cancellationToken);
        var result = sessions.Select(session => new ActiveSessionResponse(session.SessionId,
            session.DeviceName, session.DevicePlatform, session.BrowserName, session.UserAgent,
            session.FirstIpAddress, session.LastIpAddress, session.CreatedAtUtc,
            session.LastActiveAtUtc, session.ExpiresAtUtc, session.IsCurrent)).ToArray();
        return Ok(result);
    }

    [HttpDelete("others")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOtherSessions(CancellationToken cancellationToken)
    {
        if (!TryGetSessionIdentity(out var stakeholderId, out var currentSessionId))
            return Unauthorized();

        await revokeOtherSessionsHandler.HandleAsync(
            new RevokeOtherSessionsCommand(currentSessionId, stakeholderId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{sessionId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetSessionIdentity(out var stakeholderId, out _))
            return Unauthorized();

        var revoked = await revokeSessionHandler.HandleAsync(
            new RevokeSessionCommand(sessionId, stakeholderId), cancellationToken);
        if (!revoked)
            return NotFound();

        return NoContent();
    }

    private bool TryGetSessionIdentity(out Guid stakeholderId, out Guid sessionId)
    {
        var hasStakeholder = Guid.TryParse(User.FindFirst(CustomClaimTypes.StakeholderId)?.Value, out stakeholderId);
        var hasSession = Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ??
            User.FindFirst(ClaimTypes.Sid)?.Value, out sessionId);
        return hasStakeholder && hasSession;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<SignInResponse>> Handle(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignInCommand(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            ActorContext.FromAnonymousActor(currentActor));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignInStatus.Success => Ok(new SignInResponse(
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            SignInStatus.EmailNotVerified => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email not verified",
                detail: "Verify the sign-up OTP before attempting to sign in."),
            SignInStatus.AccountLocked => Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account locked",
                detail: result.LockedUntilUtc.HasValue
                    ? $"The account is locked until {DateTimeFormatter.FormatHumanReadableUtc(result.LockedUntilUtc.Value, timeProvider.GetUtcNow())}."
                    : "The account is currently locked."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The supplied email or password is invalid.")
        };
    }

    [HttpPost("google")]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<GoogleSignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<GoogleSignInResponse>> HandleGoogle(
        [FromBody] GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await googleSignInValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await googleSignInHandler.HandleAsync(
            new GoogleSignInCommand(
                request.IdToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                Request.Headers.UserAgent.ToString(),
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GoogleSignInStatus.Success => Ok(new GoogleSignInResponse(
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            GoogleSignInStatus.InvalidGoogleToken => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid Google token",
                detail: "The supplied Google identity token is invalid or expired."),
            GoogleSignInStatus.AccountNotRegistered => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Google account not registered",
                detail: "No account is linked to the supplied Google identity."),
            GoogleSignInStatus.EmailNotVerified => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email not verified",
                detail: "The linked account email must be verified before attempting to sign in."),
            GoogleSignInStatus.AccountLocked => Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account locked",
                detail: result.LockedUntilUtc.HasValue
                    ? $"The account is locked until {DateTimeFormatter.FormatHumanReadableUtc(result.LockedUntilUtc.Value, timeProvider.GetUtcNow())}."
                    : "The account is currently locked."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Google sign-in failed",
                detail: "The Google sign-in request could not be completed.")
        };
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitingPolicyNames.RefreshSessionPolicy)]
    [ProducesResponseType<RefreshSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<RefreshSessionResponse>> Refresh(
        [FromBody] RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await refreshSessionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await refreshSessionHandler.HandleAsync(
            new RefreshSessionCommand(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                Request.Headers.UserAgent.ToString(),
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            RefreshSessionStatus.Success => Ok(new RefreshSessionResponse(
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            RefreshSessionStatus.EmailNotVerified => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email not verified",
                detail: "The linked account email must be verified before attempting to refresh the session."),
            RefreshSessionStatus.AccountLocked => Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account locked",
                detail: result.LockedUntilUtc.HasValue
                    ? $"The account is locked until {DateTimeFormatter.FormatHumanReadableUtc(result.LockedUntilUtc.Value, timeProvider.GetUtcNow())}."
                    : "The account is currently locked."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid refresh token",
                detail: "The supplied refresh token is invalid, expired, or no longer active.")
        };
    }

    [HttpPost("logout")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var bearerToken = ResolveBearerToken(Request);
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Unauthorized();
        }

        JwtSecurityToken jwt;
        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(bearerToken);
        }
        catch (ArgumentException)
        {
            return Unauthorized();
        }

        DateTimeOffset? expiresAtUtc = jwt.ValidTo == DateTime.MinValue
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc));
        if (!expiresAtUtc.HasValue)
        {
            return Unauthorized();
        }

        var tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
            ?? jwt.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return Unauthorized();
        }

        if (!TryGetSessionIdentity(out var stakeholderId, out var sessionId))
        {
            return Unauthorized();
        }

        var result = await logoutSessionHandler.HandleAsync(
            new LogoutSessionCommand(tokenId, expiresAtUtc.Value, sessionId, stakeholderId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            LogoutSessionStatus.Success => NoContent(),
            _ => Unauthorized()
        };
    }

    private static string? ResolveBearerToken(HttpRequest request)
    {
        var authorizationHeader = request.Headers.Authorization.ToString();
        return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : null;
    }
}
