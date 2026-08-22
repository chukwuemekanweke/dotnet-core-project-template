using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_RotatingRefreshToken_WithFixedExpiry_Should
{
    [Fact]
    public async Task PreserveOriginalExpiry()
    {
        var now = new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero);
        var originalExpiry = now.AddDays(1);
        var user = AppUser.Create("jane@example.com");
        var currentToken = AuthenticationRefreshToken.Create(
            user.Id,
            "current-token-hash",
            "security-stamp",
            originalExpiry);
        var repository = Substitute.For<IRepository<AuthenticationRefreshToken>>();
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        identityService.GetSecurityStampAsync(user).Returns("security-stamp");
        var service = new RefreshTokenService(
            repository,
            identityService,
            Options.Create(new RefreshTokenOptions()),
            new FakeTimeProvider(now));

        var result = await service.RotateAsync(currentToken, user, CancellationToken.None);

        result.ExpiresAtUtc.ShouldBe(originalExpiry);
        currentToken.RevokedAtUtc.ShouldBe(now);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
