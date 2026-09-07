using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.ChangePassword;
using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Localization;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

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
    public IIpGeolocationService IpGeolocationService { get; } = Substitute.For<IIpGeolocationService>();
    public IRepository<Country> CountryRepository { get; } = Substitute.For<IRepository<Country>>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public RegistrationCountryValidator RegistrationCountryValidator => new(CountryRepository, IpGeolocationService);
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();

    public AuthenticationFlowTestContext()
    {
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
        CountryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg"));
    }

    public SignUpHandler CreateSignUpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        RegistrationCountryValidator,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public CheckEmailExistenceHandler CreateCheckEmailExistenceHandler() => new(IdentityService);
    public GoogleSignUpHandler CreateGoogleSignUpHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        RegistrationCountryValidator,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RequestEmailConfirmationOtpHandler CreateRequestEmailConfirmationOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        CommandSender,
        StakeholderResolver,
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
    public ChangePasswordHandler CreateChangePasswordHandler() => new(
        IdentityService,
        StakeholderReadModelRepository,
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
            AuthenticationTestData.IpAddress(),
            TestActorContext(),
            SupportedLanguages.English);
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
            AuthenticationTestData.IpAddress(),
            TestActorContext(),
            SupportedLanguages.English);

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
        string? otp = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
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

    public static ChangePasswordCommand CreateChangePasswordCommand(
        string? currentPassword = null,
        string? newPassword = null,
        ActorContext? actorContext = null)
    {
        var resolvedNewPassword = newPassword ?? "N3wP@ssword!";

        return new ChangePasswordCommand(
            currentPassword ?? AuthenticationTestData.StrongPassword(),
            resolvedNewPassword,
            resolvedNewPassword,
            actorContext ?? TestActorContext());
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


