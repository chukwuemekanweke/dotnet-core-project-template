using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.PasswordResets;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PasswordResets.Route)]
public sealed class PasswordResetsController(
    RequestPasswordResetHandler handler,
    IValidator<PasswordResetRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RequestPasswordResetResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestPasswordResetResponse>> Handle(
        [FromBody] PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(new RequestPasswordResetCommand(request.Email), cancellationToken);

        return result.Status switch
        {
            RequestPasswordResetStatus.UserNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Email not found",
                detail: "No account could be found for the supplied email address."),
            _ => Accepted((string?)null, new RequestPasswordResetResponse(
                "If the account is eligible, a password reset OTP will be sent shortly."))
        };
    }
}
