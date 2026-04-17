using System.Security.Cryptography;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Caching;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class PasswordResetOtpService(IJsonCache cache, TimeProvider timeProvider) : IPasswordResetOtpService
{
    public Task<PasswordResetOtp?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        cache.GetAsync<PasswordResetOtp>(BuildCacheKey(userId), cancellationToken);

    public async Task<PasswordResetOtp> GenerateAsync(Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var generatedOtp = new PasswordResetOtp(
            GenerateOtpCode(),
            timeProvider.GetUtcNow().Add(lifetime));

        await cache.SetAsync(BuildCacheKey(userId), generatedOtp, lifetime, cancellationToken);

        return generatedOtp;
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(BuildCacheKey(userId), cancellationToken);

    private static string BuildCacheKey(Guid userId) => $"authentication:password-reset:otp:{userId:N}";

    private static string GenerateOtpCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
