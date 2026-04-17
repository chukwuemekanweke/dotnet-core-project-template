using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.EmailConfirmations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.AuthPublicPolicy)]
[Route(EndpointUrl.EmailConfirmations.Route)]
public sealed class EmailConfirmationsController(
    SignUpOtpHandler handler,
    IValidator<SignUpOtpRequest> validator) : ControllerBase
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

        var command = new SignUpOtpCommand(request.Email, request.Otp);

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
}
