using System.Security.Cryptography;
using System.Text;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Specifications;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    IRepository<AuthenticationRefreshToken> refreshTokenRepository,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options = options.Value;

    public async Task<RefreshToken> IssueAsync(AppUser user, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var rawToken = GenerateRawToken();
        var expiresAtUtc = utcNow.AddDays(_options.LifetimeDays);
        var refreshToken = AuthenticationRefreshToken.Create(
            user.Id,
            ComputeHash(rawToken),
            user.SecurityStamp ?? string.Empty,
            expiresAtUtc,
            utcNow);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new RefreshToken(rawToken, expiresAtUtc);
    }

    public Task<AuthenticationRefreshToken?> FindByTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshTokenRepository.FirstOrDefaultAsync(
            new RefreshTokenByHashSpecification(ComputeHash(refreshToken)),
            cancellationToken);

    public async Task<RefreshToken> RotateAsync(AuthenticationRefreshToken currentRefreshToken, AppUser user, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        Revoke(currentRefreshToken, utcNow);
        refreshTokenRepository.Update(currentRefreshToken);

        return await IssueAsync(user, cancellationToken);
    }

    public void Revoke(AuthenticationRefreshToken refreshToken, DateTimeOffset utcNow)
    {
        refreshToken.Revoke(utcNow);
        refreshTokenRepository.Update(refreshToken);
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}
