using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Application.Authentication.Features.ChangePassword;
using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.ListSessions;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.RevokeOtherSessions;
using BackendProjectTemplate.Application.Authentication.Features.RevokeSession;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication;

internal sealed class AuthenticationControllerTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAuthenticationSessionService SessionService { get; } = Substitute.For<IAuthenticationSessionService>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IIpGeolocationService IpGeolocationService { get; } = Substitute.For<IIpGeolocationService>();
    public IRepository<Country> CountryRepository { get; } = Substitute.For<IRepository<Country>>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public RegistrationCountryValidator RegistrationCountryValidator => new(CountryRepository, IpGeolocationService);

    public AuthenticationControllerTestContext()
    {
        RefreshTokenService.GetExpiry(Arg.Any<TimeSpan?>()).Returns(Clock.GetUtcNow().AddDays(30));
        SessionService.CreateAsync(Arg.Any<AppUser>(), Arg.Any<Stakeholder>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(call => AuthenticationSession.Create(((AppUser)call[0]).Id,
                ((Stakeholder)call[1]).Id, ((Stakeholder)call[1]).TenantId, Guid.CreateVersion7(),
                (string)call[3], null, null, null, Clock.GetUtcNow(), (DateTimeOffset)call[4]));
        CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        CurrentActor.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        CurrentActor.FlowId.Returns(Guid.CreateVersion7().ToString("N"));
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

    public SignInHandler CreateSignInHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        SessionService,
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
        SessionService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public RefreshSessionHandler CreateRefreshSessionHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        SessionService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CustomTelemetryContext,
        SessionService,
        UnitOfWork);

    public SessionsController CreateSessionsController(ClaimsPrincipal user) => new(
        CreateSignInHandler(),
        CreateGoogleSignInHandler(),
        CreateLogoutSessionHandler(),
        CreateRefreshSessionHandler(),
        Substitute.For<IValidator<SignInRequest>>(),
        Substitute.For<IValidator<GoogleSignInRequest>>(),
        Substitute.For<IValidator<RefreshSessionRequest>>(),
        Clock,
        CurrentActor,
        CreateListSessionsHandler(),
        CreateRevokeSessionHandler(),
        CreateRevokeOtherSessionsHandler())
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
    };

    public ListSessionsHandler CreateListSessionsHandler() => new(SessionService);
    public RevokeSessionHandler CreateRevokeSessionHandler() => new(SessionService, UnitOfWork);
    public RevokeOtherSessionsHandler CreateRevokeOtherSessionsHandler() => new(SessionService, UnitOfWork);

    public static ClaimsPrincipal CreateSessionPrincipal(Guid userId, Guid stakeholderId, Guid sessionId) =>
        new(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(CustomClaimTypes.StakeholderId, stakeholderId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId.ToString())
        ], "Bearer"));

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
        UnitOfWork,
        SessionService);

    public ChangePasswordHandler CreateChangePasswordHandler() => new(
        IdentityService,
        StakeholderReadModelRepository,
        CustomTelemetryContext,
        UnitOfWork);

    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        AccessTokenService,
        RefreshTokenService,
        SessionService,
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

    public AppUser CreateUser(string? email = null, string? firstName = null, string? lastName = null) =>
        AppUser.Create(
            email ?? "jane@example.com",
            firstName ?? "Jane",
            lastName ?? "Doe");

    public Stakeholder CreateStakeholder(Guid appUserId) =>
        Stakeholder.Create(
            appUserId,
            CurrentActor.TenantId!.Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");

    public StakeholderType CreateStakeholderType() =>
        StakeholderType.Create(
            CurrentActor.TenantId!.Value,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}





