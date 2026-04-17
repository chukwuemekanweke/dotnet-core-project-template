using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserCreated_ShouldGenerateAndDeliverSignUpOtp
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var otpDeliveryService = Substitute.For<IOtpDeliveryService>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var otpCode = ConsumerTestData.Otp();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, tenantId, countryId, Guid.CreateVersion7(), firstName, lastName, null, false));
        identityService.FindByIdAsync(user.Id).Returns(user);
        identityService.GenerateSignUpOtpAsync(user).Returns(otpCode);

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            otpDeliveryService,
            logger).HandleAsync(
            new UserCreated(email)
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await identityService.Received(1).GenerateSignUpOtpAsync(user);
        await identityService.Received(1).FindByIdAsync(user.Id);
        await otpDeliveryService.Received(1).SendSignUpOtpAsync(user, otpCode, Arg.Any<CancellationToken>());
    }
}
