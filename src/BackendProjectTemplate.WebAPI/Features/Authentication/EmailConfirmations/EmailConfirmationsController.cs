using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.EmailConfirmations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.EmailOperationsPolicy)]
[Route(EndpointUrl.EmailConfirmations.Route)]
public sealed class EmailConfirmationsController(
    SignUpOtpHandler handler,
    RequestEmailConfirmationOtpHandler requestEmailConfirmationOtpHandler,
    IValidator<SignUpOtpRequest> validator,
    IValidator<RequestEmailConfirmationOtpRequest> requestEmailConfirmationOtpValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpOtpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignUpOtpResponse>> Handle(
        [FromBody] SignUpOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignUpOtpCommand(request.Email, request.Otp, ActorContext.FromAnonymousActor(currentActor));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpOtpStatus.Success => Ok(new SignUpOtpResponse("OTP verified. You can now sign in.")),
            SignUpOtpStatus.AlreadyVerified => Ok(new SignUpOtpResponse("The account was already verified.")),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid OTP",
                detail: "The OTP is invalid, expired, or has already been consumed.")
        };
    }

    [HttpPost("confirmation-code")]
    [ProducesResponseType<RequestEmailConfirmationOtpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestEmailConfirmationOtpResponse>> RequestCode(
        [FromBody] RequestEmailConfirmationOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await requestEmailConfirmationOtpValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await requestEmailConfirmationOtpHandler.HandleAsync(
            new RequestEmailConfirmationOtpCommand(request.Email, ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        var message = result.Status switch
        {
            RequestEmailConfirmationOtpStatus.AlreadyVerified => "The account was already verified.",
            _ => "If the account exists and still requires verification, an OTP will be sent when no active code exists."
        };

        return Ok(new RequestEmailConfirmationOtpResponse(message, result.RetryAtUtc));
    }
}
