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
    IPasswordResetOtpService passwordResetOtpService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork) : BaseMessageHandler<ResetPasswordCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(2);

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

        var activeOtp = await passwordResetOtpService.GetActiveAsync(stakeholder.AppUserId, cancellationToken);
        if (activeOtp is not null)
        {
            return;
        }

        var otp = await passwordResetOtpService.GenerateAsync(stakeholder.AppUserId, OtpLifetime, cancellationToken);

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
            Observability.EventNames.Authentication.PasswordResetRequested,
            new Dictionary<string, string>
            {
                [Observability.StakeholderIdPropertyName] = stakeholder.StakeholderId.ToString()
            });
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(ResetPasswordCommand message)
    {
        yield break;
    }
}
