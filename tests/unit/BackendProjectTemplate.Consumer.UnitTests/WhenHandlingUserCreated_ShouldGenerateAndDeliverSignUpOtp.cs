using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
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
        var otpDeliveryService = Substitute.For<IOtpDeliveryService>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var userId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var otpCode = ConsumerTestData.Otp();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        identityService.FindByIdAsync(userId).Returns(user);
        identityService.GenerateSignUpOtpAsync(user).Returns(otpCode);

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            identityService,
            stakeholderReadModelRepository,
            otpDeliveryService,
            logger).HandleAsync(
            new UserCreated(userId, email),
            CancellationToken.None);

        await identityService.Received(1).GenerateSignUpOtpAsync(user);
        await otpDeliveryService.Received(1).SendSignUpOtpAsync(user, otpCode, Arg.Any<CancellationToken>());
    }
}
