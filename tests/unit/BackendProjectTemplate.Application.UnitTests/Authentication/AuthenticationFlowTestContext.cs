using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using NSubstitute;

namespace BackendProjectTemplate.Application.UnitTests.Authentication;

internal sealed class AuthenticationFlowTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IOtpDeliveryService OtpDeliveryService { get; } = Substitute.For<IOtpDeliveryService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();

    public AuthenticationFlowTestContext()
    {
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
    }

    public SignUpHandler CreateSignUpHandler() => new(IdentityService, EventPublisher, CustomTelemetryContext, UnitOfWork, Clock);
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(IdentityService, EventPublisher, CustomTelemetryContext, UnitOfWork, Clock);
    public SignInHandler CreateSignInHandler() => new(IdentityService, AccessTokenService, EventPublisher, CustomTelemetryContext, UnitOfWork, Clock);

    public static SignUpCommand CreateSignUpCommand(
        string? email = null,
        string? password = null,
        string? firstName = null,
        string? lastName = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new SignUpCommand(
            email ?? AuthenticationTestData.Email(),
            resolvedPassword,
            resolvedPassword,
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName());
    }

    public static SignInCommand CreateSignInCommand(
        string? email = null,
        string? password = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            password ?? AuthenticationTestData.StrongPassword(),
            ipAddress ?? "127.0.0.1",
            userAgent ?? "UnitTestAgent/1.0");

    public static SignUpOtpCommand CreateSignUpOtpCommand(
        string? email = null,
        string? otp = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp());

    public AppUser CreateUser(
        string? email = null,
        string? firstName = null,
        string? lastName = null) =>
        AppUser.Create(
            email ?? AuthenticationTestData.Email(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            Clock.GetUtcNow());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
