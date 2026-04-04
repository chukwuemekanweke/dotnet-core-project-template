using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.UnitTests;

public sealed class AuthenticationFlowTests
{
    [Fact]
    public async Task SignUp_CreatesPendingUserAndOtp()
    {
        var clock = new Fakes.FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var users = new Fakes.FakeRepository<AppUser>();
        var otps = new Fakes.FakeRepository<SignUpOtp>();
        var unitOfWork = new Fakes.FakeUnitOfWork();
        var delivery = new Fakes.FakeOtpDeliveryService();

        var handler = new SignUpHandler(
            users,
            otps,
            unitOfWork,
            new PasswordHasher(),
            new OtpCodeService(),
            delivery,
            clock);

        var result = await handler.HandleAsync(new SignUpRequest
        {
            Email = "ada@example.com",
            Password = "P@ssw0rd123!",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, CancellationToken.None);

        Assert.Equal(SignUpStatus.Accepted, result.Status);
        Assert.Single(users.Items);
        Assert.Single(otps.Items);
        Assert.NotNull(delivery.GetCode("ada@example.com"));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task SignUp_ReturnsDuplicateEmail_WhenUserAlreadyExists()
    {
        var clock = new Fakes.FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var existingUser = AppUser.Create(
            "ada@example.com",
            "Ada",
            "Lovelace",
            "hash",
            "salt",
            clock.GetUtcNow());

        var handler = new SignUpHandler(
            new Fakes.FakeRepository<AppUser>([existingUser]),
            new Fakes.FakeRepository<SignUpOtp>(),
            new Fakes.FakeUnitOfWork(),
            new PasswordHasher(),
            new OtpCodeService(),
            new Fakes.FakeOtpDeliveryService(),
            clock);

        var result = await handler.HandleAsync(new SignUpRequest
        {
            Email = "ada@example.com",
            Password = "P@ssw0rd123!",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, CancellationToken.None);

        Assert.Equal(SignUpStatus.DuplicateEmail, result.Status);
    }

    [Fact]
    public async Task VerifyOtp_MarksUserAsVerified()
    {
        var clock = new Fakes.FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var users = new Fakes.FakeRepository<AppUser>();
        var otps = new Fakes.FakeRepository<SignUpOtp>();
        var unitOfWork = new Fakes.FakeUnitOfWork();
        var delivery = new Fakes.FakeOtpDeliveryService();

        var signUpHandler = new SignUpHandler(
            users,
            otps,
            unitOfWork,
            new PasswordHasher(),
            new OtpCodeService(),
            delivery,
            clock);

        await signUpHandler.HandleAsync(new SignUpRequest
        {
            Email = "grace@example.com",
            Password = "P@ssw0rd123!",
            FirstName = "Grace",
            LastName = "Hopper"
        }, CancellationToken.None);

        var verifyHandler = new SignUpOtpHandler(
            users,
            otps,
            unitOfWork,
            new OtpCodeService(),
            clock);

        var result = await verifyHandler.HandleAsync(new SignUpOtpRequest
        {
            Email = "grace@example.com",
            Otp = delivery.GetCode("grace@example.com")!
        }, CancellationToken.None);

        Assert.Equal(SignUpOtpStatus.Success, result.Status);
        Assert.Single(users.Items, user => user.IsEmailVerified);
    }

    [Fact]
    public async Task SignIn_ReturnsAccessToken_ForVerifiedUser()
    {
        var clock = new Fakes.FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var passwordHasher = new PasswordHasher();
        var (hash, salt) = passwordHasher.HashPassword("P@ssw0rd123!");
        var user = AppUser.Create("linus@example.com", "Linus", "Torvalds", hash, salt, clock.GetUtcNow());
        user.MarkEmailVerified(clock.GetUtcNow());

        var handler = new SignInHandler(
            new Fakes.FakeRepository<AppUser>([user]),
            passwordHasher,
            new JwtTokenGenerator(
                Options.Create(new JwtOptions
                {
                    Issuer = "tests",
                    Audience = "tests",
                    SigningKey = "super-secret-template-signing-key-change-me"
                }),
                clock));

        var result = await handler.HandleAsync(new SignInRequest
        {
            Email = "linus@example.com",
            Password = "P@ssw0rd123!"
        }, CancellationToken.None);

        Assert.Equal(SignInStatus.Success, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken?.Value));
    }
}
