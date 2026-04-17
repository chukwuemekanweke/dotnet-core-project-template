namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IPasswordResetOtpService
{
    Task<PasswordResetOtp?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PasswordResetOtp> GenerateAsync(Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
}
