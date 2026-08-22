using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_IssuingRefreshToken_WithCustomLifetime_Should
{
    [Fact]
    public async Task UseRequestedExpiry()
    {
        var now = new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero);
        var lifetime = TimeSpan.FromDays(1);
        var user = AppUser.Create("jane@example.com");
        var repository = Substitute.For<IRepository<AuthenticationRefreshToken>>();
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        identityService.GetSecurityStampAsync(user).Returns("security-stamp");
        var service = new RefreshTokenService(
            repository,
            identityService,
            Options.Create(new RefreshTokenOptions()),
            new FakeTimeProvider(now));

        var result = await service.IssueAsync(user, lifetime, CancellationToken.None);

        result.ExpiresAtUtc.ShouldBe(now.Add(lifetime));
        await repository.Received(1).AddAsync(Arg.Is<AuthenticationRefreshToken>(token =>
            token.AppUserId == user.Id
            && token.ExpiresAtUtc == now.Add(lifetime)));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
