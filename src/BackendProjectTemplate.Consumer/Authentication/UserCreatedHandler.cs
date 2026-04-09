using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserCreatedHandler(
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationIdentityService identityService,
    IOtpDeliveryService otpDeliveryService,
    ILogger<UserCreatedHandler> logger) : BaseMessageHandler<UserCreated>(customTelemetryContext)
{
    protected override async Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(message.UserId);
        if (user is null)
        {
            logger.LogWarning(
                "Unable to send sign-up OTP for user {UserId} because the account could not be found.",
                message.UserId);

            return;
        }

        if (user.EmailConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP delivery for user {UserId} because the email is already confirmed.",
                message.UserId);

            return;
        }

        var otpCode = await identityService.GenerateSignUpOtpAsync(user);
        await otpDeliveryService.SendSignUpOtpAsync(user, otpCode, cancellationToken);
        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserCreatedProcessed, new Dictionary<string, string>
        {
            [Observability.UserIdPropertyName] = user.Id.ToString()
        });
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserCreated message)
    {
        yield return (Observability.UserIdPropertyName, message.UserId.ToString());
    }
}
