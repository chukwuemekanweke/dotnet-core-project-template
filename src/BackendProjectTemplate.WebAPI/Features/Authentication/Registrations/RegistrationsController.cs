using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Registrations.Route)]
public sealed class RegistrationsController(
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

        var command = new SignUpCommand(
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.FirstName,
            request.LastName);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            SignUpStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            _ => Accepted((string?)null, new SignUpResponse(
                request.Email,
                "The sign-up request has been accepted. The account verification OTP will be sent shortly."))
        };
    }
}
