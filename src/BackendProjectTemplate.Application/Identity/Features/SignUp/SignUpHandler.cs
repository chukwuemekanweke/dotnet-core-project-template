using BackendProjectTemplate.Application.Identity.Specifications;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Identity.Entities;
using SignUpOtpEntity = BackendProjectTemplate.Domain.Identity.Entities.SignUpOtp;

namespace BackendProjectTemplate.Application.Identity.Features.SignUp;

public sealed class SignUpHandler(
    IRepository<AppUser> users,
    IRepository<SignUpOtpEntity> signUpOtps,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IOtpCodeService otpCodeService,
    IOtpDeliveryService otpDeliveryService,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    public async Task<SignUpResult> HandleAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = AppUser.NormalizeEmail(request.Email);

        if (await users.AnyAsync(new UserByNormalizedEmailSpecification(normalizedEmail), cancellationToken))
        {
            return new SignUpResult(SignUpStatus.DuplicateEmail, null);
        }

        var now = timeProvider.GetUtcNow();
        var (passwordHash, passwordSalt) = passwordHasher.HashPassword(request.Password);

        var user = AppUser.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            passwordSalt,
            now);

        var otpCode = otpCodeService.GenerateCode();
        var otpHash = otpCodeService.HashCode(normalizedEmail, otpCode);
        var otp = SignUpOtpEntity.Create(user.Id, normalizedEmail, otpHash, now.Add(OtpLifetime), now);

        await users.AddAsync(user, cancellationToken);
        await signUpOtps.AddAsync(otp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await otpDeliveryService.SendSignUpOtpAsync(user, otpCode, cancellationToken);

        return new SignUpResult(SignUpStatus.Accepted, otp.ExpiresAtUtc);
    }
}

public sealed record SignUpResult(SignUpStatus Status, DateTimeOffset? OtpExpiresAtUtc);

public enum SignUpStatus
{
    Accepted = 1,
    DuplicateEmail = 2
}
