using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.UnitTests;

public sealed class AuthenticationFlowTests
{
    [Fact]
    public async Task SignUp_CreatesIdentityUser_AndSendsOtp()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var otpDeliveryService = Substitute.For<IOtpDeliveryService>();

        identityService.FindByEmailAsync("ada@example.com").Returns((AppUser?)null);
        identityService.CreateAsync(Arg.Any<AppUser>(), "P@ssw0rd123!").Returns(IdentityResult.Success);
        identityService.GenerateSignUpOtpAsync(Arg.Any<AppUser>()).Returns("123456");

        var handler = new SignUpHandler(identityService, otpDeliveryService, clock);
        var result = await handler.HandleAsync(new SignUpRequest
        {
            Email = "ada@example.com",
            Password = "P@ssw0rd123!",
            ConfirmPassword = "P@ssw0rd123!",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        result.OtpExpiresAtUtc.ShouldBe(clock.GetUtcNow().AddMinutes(3));
        await identityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == "ada@example.com" &&
                user.UserName == "ada@example.com" &&
                user.FirstName == "Ada" &&
                user.LastName == "Lovelace"),
            "P@ssw0rd123!");
        await otpDeliveryService.Received(1).SendSignUpOtpAsync(
            Arg.Is<AppUser>(user => user.Email == "ada@example.com"),
            "123456",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignUp_ReturnsValidationFailure_WhenIdentityRejectsThePassword()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var otpDeliveryService = Substitute.For<IOtpDeliveryService>();

        identityService.FindByEmailAsync("ada@example.com").Returns((AppUser?)null);
        identityService.CreateAsync(Arg.Any<AppUser>(), "weakpass").Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
                Description = "Passwords must have at least one digit ('0'-'9')."
            }));

        var handler = new SignUpHandler(identityService, otpDeliveryService, TimeProvider.System);
        var result = await handler.HandleAsync(new SignUpRequest
        {
            Email = "ada@example.com",
            Password = "weakpass",
            ConfirmPassword = "weakpass",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.ValidationFailed);
        result.ValidationErrors.ShouldNotBeNull();
        result.ValidationErrors.ShouldContainKey(nameof(SignUpRequest.Password));
        await otpDeliveryService.DidNotReceive().SendSignUpOtpAsync(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtp_MarksUserAsVerified()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var user = AppUser.Create("grace@example.com", "Grace", "Hopper", clock.GetUtcNow());
        var identityService = Substitute.For<IAuthenticationIdentityService>();

        identityService.FindByEmailAsync("grace@example.com").Returns(user);
        identityService.VerifySignUpOtpAsync(user, "123456").Returns(true);
        identityService.UpdateAsync(Arg.Is<AppUser>(candidate => candidate.EmailConfirmed)).Returns(IdentityResult.Success);

        var handler = new SignUpOtpHandler(identityService, clock);
        var result = await handler.HandleAsync(new SignUpOtpRequest
        {
            Email = "grace@example.com",
            Otp = "123456"
        }, CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.Success);
        user.EmailConfirmed.ShouldBeTrue();
        user.UpdatedAtUtc.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public async Task SignIn_ReturnsAccessToken_ForConfirmedIdentityUser()
    {
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create("linus@example.com", "Linus", "Torvalds", now);
        user.MarkEmailVerified(now);

        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var accessTokenService = Substitute.For<IAccessTokenService>();
        var expectedToken = new AccessToken("signed-jwt", now.AddHours(1));

        identityService.FindByEmailAsync("linus@example.com").Returns(user);
        identityService.CheckPasswordAsync(user, "P@ssw0rd123!").Returns(true);
        accessTokenService.Generate(user).Returns(expectedToken);

        var handler = new SignInHandler(identityService, accessTokenService);
        var result = await handler.HandleAsync(new SignInRequest
        {
            Email = "linus@example.com",
            Password = "P@ssw0rd123!"
        }, CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.Success);
        result.AccessToken.ShouldBe(expectedToken);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
