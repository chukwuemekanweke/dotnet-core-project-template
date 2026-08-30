using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.ChangePassword;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passwords;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Passwords.Route)]
public sealed class PasswordsController(
    ChangePasswordHandler handler,
    IValidator<ChangePasswordRequest> validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Change(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            ChangePasswordStatus.Success => NoContent(),
            ChangePasswordStatus.NotAuthenticated => Unauthorized(),
            ChangePasswordStatus.UserNotFound => NotFound(),
            ChangePasswordStatus.IncorrectCurrentPassword => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Current password is incorrect",
                detail: "Current password is incorrect"),
            _ => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(
                    result.ValidationErrors ?? new Dictionary<string, string[]>())))
        };
    }
}
