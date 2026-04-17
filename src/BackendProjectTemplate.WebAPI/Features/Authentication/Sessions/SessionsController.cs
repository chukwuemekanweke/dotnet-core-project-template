using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
[Route(EndpointUrl.Sessions.Route)]
public sealed class SessionsController(
    SignInHandler handler,
    IValidator<SignInRequest> validator) : ControllerBase
{
    [HttpPost]
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
            Request.Headers.UserAgent.ToString());

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignInStatus.Success => Ok(new SignInResponse(
                result.AccessToken!.Value,
                result.AccessToken.ExpiresAtUtc,
                "Bearer")),
            SignInStatus.EmailNotVerified => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email not verified",
                detail: "Verify the sign-up OTP before attempting to sign in."),
            SignInStatus.AccountLocked => Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account locked",
                detail: result.LockedUntilUtc.HasValue
                    ? $"The account is locked until {result.LockedUntilUtc.Value:O}."
                    : "The account is currently locked."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The supplied email or password is invalid.")
        };
    }
}
