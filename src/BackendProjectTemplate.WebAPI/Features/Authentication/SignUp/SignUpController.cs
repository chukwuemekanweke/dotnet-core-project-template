using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.SignUp;

[ApiController]
[Route("api/authentication/sign-up")]
public sealed class SignUpController(
    SignUpHandler handler,
    IValidator<SignUpRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignUpResponse>> Handle(
        [FromBody] SignUpRequest request,
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
            SignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            _ => Accepted((string?)null, new SignUpResponse(
                request.Email,
                result.OtpExpiresAtUtc!.Value,
                "The sign-up request has been accepted. Verify the OTP to activate the account."))
        };
    }
}
