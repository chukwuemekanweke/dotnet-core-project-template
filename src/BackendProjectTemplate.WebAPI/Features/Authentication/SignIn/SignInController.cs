using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.SignIn;

[ApiController]
[Route("api/authentication/sign-in")]
public sealed class SignInController(
    SignInHandler handler,
    IValidator<SignInRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SignInResponse>> Handle(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(request, cancellationToken);

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
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The supplied email or password is invalid.")
        };
    }
}
