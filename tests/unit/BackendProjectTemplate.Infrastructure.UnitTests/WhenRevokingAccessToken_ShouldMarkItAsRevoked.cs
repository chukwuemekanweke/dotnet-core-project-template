using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Infrastructure.Authentication;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenRevokingAccessToken_ShouldMarkItAsRevoked
{
    [Fact]
    public async Task Verify()
    {
        var cache = Substitute.For<IJsonCache>();
        var service = new AccessTokenRevocationService(cache);
        var tokenId = Guid.CreateVersion7().ToString("N");
        var expectedCacheKey = $"authentication:access-token:revoked:{tokenId}";

        cache.GetAsync<bool>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<string>() == expectedCacheKey);

        await service.RevokeAsync(tokenId, DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        await cache.Received(1).SetAsync(
            expectedCacheKey,
            true,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
        (await service.IsRevokedAsync(tokenId, CancellationToken.None)).ShouldBeTrue();
    }
}
