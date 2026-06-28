using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;

namespace BackendProjectTemplate.Application.UnitTests.Authentication;

internal sealed class AuthenticationFlowTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();

    public AuthenticationFlowTestContext()
    {
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
    }

    public SignUpHandler CreateSignUpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        UnitOfWork);
    public GoogleSignUpHandler CreateGoogleSignUpHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        UnitOfWork);
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public SignInHandler CreateSignInHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public GoogleSignInHandler CreateGoogleSignInHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RefreshSessionHandler CreateRefreshSessionHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RequestPasswordResetHandler CreateRequestPasswordResetHandler() => new(
        IdentityService,
        CommandSender,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);
    public CompletePasswordResetHandler CreateCompletePasswordResetHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);
    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CustomTelemetryContext);

    private static ActorContext TestActorContext() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7().ToString("N"),
        Guid.CreateVersion7().ToString("N"));

    public static SignUpCommand CreateSignUpCommand(
        string? email = null,
        string? password = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new SignUpCommand(
            email ?? AuthenticationTestData.Email(),
            resolvedPassword,
            resolvedPassword,
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            TestActorContext());
    }

    public static SignInCommand CreateSignInCommand(
        string? email = null,
        string? password = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            password ?? AuthenticationTestData.StrongPassword(),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static GoogleSignUpCommand CreateGoogleSignUpCommand(
        string? idToken = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null) =>
        new(
            idToken ?? "google-id-token",
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            TestActorContext());

    public static GoogleSignInCommand CreateGoogleSignInCommand(
        string? idToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            idToken ?? "google-id-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static SignUpOtpCommand CreateSignUpOtpCommand(
        string? email = null,
        string? otp = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            TestActorContext());

    public static RequestPasswordResetCommand CreateRequestPasswordResetCommand(string? email = null) =>
        new(email ?? AuthenticationTestData.Email(), TestActorContext());

    public static CompletePasswordResetCommand CreateCompletePasswordResetCommand(
        string? email = null,
        string? otp = null,
        string? password = null,
        string? confirmPassword = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new CompletePasswordResetCommand(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            resolvedPassword,
            confirmPassword ?? resolvedPassword,
            TestActorContext());
    }

    public static RefreshSessionCommand CreateRefreshSessionCommand(
        string? refreshToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            refreshToken ?? "refresh-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public AppUser CreateUser(
        string? email = null,
        string? firstName = null,
        string? lastName = null) =>
        AppUser.Create(
            email ?? AuthenticationTestData.Email(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}


