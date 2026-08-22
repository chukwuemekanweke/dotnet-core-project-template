using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Formatting;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class EmailConfirmationOtpSender(
    ITwoFactorOtpService twoFactorOtpService,
    ICommandSender commandSender,
    TimeProvider timeProvider)
{
    public async Task<EmailConfirmationOtpSendStatus> SendAsync(
        StakeholderReadModel stakeholder,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (await twoFactorOtpService.OtpExistsAsync(
                stakeholder.AppUserId,
                OtpIntent.EmailConfirmation,
                cancellationToken))
        {
            return EmailConfirmationOtpSendStatus.ActiveOtpExists;
        }

        var remainingLifetime = expiresAtUtc - timeProvider.GetUtcNow();
        if (remainingLifetime <= AuthenticationOtpDefaults.EmailConfirmationDeliverySafetyThreshold)
        {
            return EmailConfirmationOtpSendStatus.InsufficientLifetime;
        }

        var otp = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.EmailConfirmation,
            cancellationToken,
            characterLength: 6,
            isAlphaNumeric: false,
            expiresAtUtc: expiresAtUtc);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.TenantId,
                stakeholder.CountryId,
                NotificationType.EmailConfirmationOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otp.Code,
                        ["OtpExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(
                            otp.ExpiresAtUtc,
                            timeProvider.GetUtcNow()),
                        ["VerifyUrl"] = string.Empty,
                        ["Product"] = "BackendProjectTemplate"
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);

        return EmailConfirmationOtpSendStatus.Sent;
    }
}
