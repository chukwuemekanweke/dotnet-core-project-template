using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using NSubstitute;

namespace BackendProjectTemplate.Application.UnitTests.Authentication;

internal sealed class AuthenticationFlowTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IOtpDeliveryService OtpDeliveryService { get; } = Substitute.For<IOtpDeliveryService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IOutboxWriter OutboxWriter { get; } = Substitute.For<IOutboxWriter>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();

    public AuthenticationFlowTestContext()
    {
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
    }

    public SignUpHandler CreateSignUpHandler() => new(IdentityService, OtpDeliveryService, OutboxWriter, UnitOfWork, Clock);
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(IdentityService, OutboxWriter, UnitOfWork, Clock);
    public SignInHandler CreateSignInHandler() => new(IdentityService, AccessTokenService);

    public static SignUpRequest CreateSignUpRequest(
        string email = "ada@example.com",
        string password = "P@ssw0rd123!",
        string firstName = "Ada",
        string lastName = "Lovelace") =>
        new()
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = firstName,
            LastName = lastName
        };

    public static SignInRequest CreateSignInRequest(
        string email = "linus@example.com",
        string password = "P@ssw0rd123!") =>
        new()
        {
            Email = email,
            Password = password
        };

    public static SignUpOtpRequest CreateSignUpOtpRequest(
        string email = "grace@example.com",
        string otp = "123456") =>
        new()
        {
            Email = email,
            Otp = otp
        };

    public AppUser CreateUser(
        string email = "grace@example.com",
        string firstName = "Grace",
        string lastName = "Hopper") =>
        AppUser.Create(email, firstName, lastName, Clock.GetUtcNow());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
