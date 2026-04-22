using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
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

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication;

internal sealed class AuthenticationControllerTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);

    public AuthenticationControllerTestContext()
    {
        CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        CurrentActor.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        CurrentActor.FlowId.Returns(Guid.CreateVersion7().ToString("N"));
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
    }

    public SignUpHandler CreateSignUpHandler() => new(
        IdentityService,
        EventPublisher,
        CurrentActor,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public GoogleSignUpHandler CreateGoogleSignUpHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        EventPublisher,
        CurrentActor,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public SignInHandler CreateSignInHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CurrentActor,
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
        CurrentActor,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public RefreshSessionHandler CreateRefreshSessionHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CurrentActor,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CurrentActor,
        CustomTelemetryContext);

    public RequestPasswordResetHandler CreateRequestPasswordResetHandler() => new(
        IdentityService,
        CommandSender,
        StakeholderResolver,
        CurrentActor,
        CustomTelemetryContext,
        UnitOfWork);

    public CompletePasswordResetHandler CreateCompletePasswordResetHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        StakeholderResolver,
        CurrentActor,
        CustomTelemetryContext,
        UnitOfWork);

    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderResolver,
        CurrentActor,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public AppUser CreateUser(string? email = null, string? firstName = null, string? lastName = null) =>
        AppUser.Create(
            email ?? "jane@example.com",
            firstName ?? "Jane",
            lastName ?? "Doe",
            Clock.GetUtcNow());

    public Stakeholder CreateStakeholder(Guid appUserId) =>
        Stakeholder.Create(
            appUserId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe",
            Clock.GetUtcNow());

    public StakeholderType CreateStakeholderType() =>
        StakeholderType.Create(
            CurrentActor.TenantId!.Value,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey,
            Clock.GetUtcNow());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
