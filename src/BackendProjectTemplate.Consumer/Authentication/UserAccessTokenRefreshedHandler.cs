using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserAccessTokenRefreshedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ILoginActivityIpAddressResolver loginActivityIpAddressResolver,
    IRepository<LoginActivity> loginActivityRepository,
    IUnitOfWork unitOfWork,
    IUserAgentParserService userAgentParserService,
    TimeProvider timeProvider) : BaseMessageHandler<UserAccessTokenRefreshed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(UserAccessTokenRefreshed message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserAccessTokenRefreshed must contain a valid stakeholder actor id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserAccessTokenRefreshed because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var userAgentInfo = userAgentParserService.Parse(message.UserAgent);
        var ipAddressResolution = await loginActivityIpAddressResolver.ResolveAsync(message.IpAddress, cancellationToken);

        var loginActivity = LoginActivity.CreateTokenRefresh(
            stakeholder.StakeholderId,
            stakeholder.TenantId,
            ipAddressResolution.IpAddressId,
            ipAddressResolution.IpAddressLocationId,
            message.UserAgent,
            userAgentInfo.DeviceName,
            userAgentInfo.DevicePlatform,
            userAgentInfo.BrowserName,
            timeProvider.GetUtcNow());

        await loginActivityRepository.AddAsync(loginActivity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserAccessTokenRefreshed, new Dictionary<string, string>
        {
            [Observability.StakeholderIdPropertyName] = stakeholder.StakeholderId.ToString()
        });
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserAccessTokenRefreshed message)
    {
        yield break;
    }
}
