using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class ResetPasswordHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ITwoFactorOtpService twoFactorOtpService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork) : BaseMessageHandler<ResetPasswordCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(ResetPasswordCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("ResetPasswordCommand must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process ResetPasswordCommand because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (await twoFactorOtpService.OtpExistsAsync(stakeholder.AppUserId, OtpIntent.PasswordReset, cancellationToken))
        {
            return;
        }

        var otp = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.PasswordReset,
            cancellationToken,
            characterLength: 6,
            isAlphaNumeric: false);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.TenantId,
                stakeholder.CountryId,
                NotificationType.ResetPasswordOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otp.Code,
                        ["OtpExpiresAtUtc"] = otp.ExpiresAtUtc.ToString("O")
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetOtpSent,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(ResetPasswordCommand message)
    {
        yield break;
    }
}
