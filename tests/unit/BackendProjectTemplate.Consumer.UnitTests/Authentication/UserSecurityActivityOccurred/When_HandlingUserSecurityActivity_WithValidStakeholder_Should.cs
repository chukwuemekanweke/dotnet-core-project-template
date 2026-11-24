using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication.UserSecurityActivityOccurred;

public sealed class When_HandlingUserSecurityActivity_WithValidStakeholder_Should
{
    [Theory]
    [InlineData(UserSecurityActivityTypes.PasskeyAdded, SecurityActivityType.PasskeyAdded)]
    [InlineData(UserSecurityActivityTypes.PasskeyRemoved, SecurityActivityType.PasskeyRemoved)]
    [InlineData(UserSecurityActivityTypes.TwoFactorEnabled, SecurityActivityType.TwoFactorEnabled)]
    [InlineData(UserSecurityActivityTypes.TwoFactorDisabled, SecurityActivityType.TwoFactorDisabled)]
    [InlineData(UserSecurityActivityTypes.TwoFactorRecoveryCodesRegenerated, SecurityActivityType.TwoFactorRecoveryCodesRegenerated)]
    [InlineData(UserSecurityActivityTypes.PasswordChanged, SecurityActivityType.PasswordChanged)]
    public async Task PersistMappedSecurityActivity(string activityType, SecurityActivityType expectedActivityType)
    {
        var context = new UserSecurityActivityOccurredTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var ipAddressId = Guid.CreateVersion7();
        var ipAddressLocationId = Guid.CreateVersion7();
        var stakeholder = context.ArrangeStakeholder(stakeholderId);
        var message = UserSecurityActivityOccurredTestContext.CreateMessage(activityType, stakeholderId);
        context.IpAddressResolver.ResolveAsync(message.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityIpAddressResolution(ipAddressId, ipAddressLocationId));
        context.UserAgentParserService.Parse(message.UserAgent)
            .Returns(new UserAgentInfo("Desktop", "Windows", "Chrome"));

        await context.CreateHandler().HandleAsync(message, CancellationToken.None);

        await context.SecurityActivityRepository.Received(1).AddAsync(
            Arg.Is<SecurityActivity>(activity =>
                activity.ActivityType == expectedActivityType &&
                activity.StakeholderId == stakeholderId &&
                activity.TenantId == stakeholder.TenantId &&
                activity.IpAddressId == ipAddressId &&
                activity.IpAddressLocationId == ipAddressLocationId &&
                activity.DeviceName == "Desktop" &&
                activity.BrowserName == "Chrome" &&
                activity.OccurredAtUtc == message.OccuredAt &&
                activity.FailureReason == null),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
