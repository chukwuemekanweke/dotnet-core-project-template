using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserCreatedHandler(
    IAuthenticationIdentityService identityService,
    IOtpDeliveryService otpDeliveryService,
    ILogger<UserCreatedHandler> logger) : IMessageHandler<UserCreated>
{
    public async Task HandleAsync(UserCreated message, CancellationToken cancellationToken)
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
    }
}
