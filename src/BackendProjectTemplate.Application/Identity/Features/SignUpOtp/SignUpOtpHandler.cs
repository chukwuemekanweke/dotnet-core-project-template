using BackendProjectTemplate.Application.Identity.Specifications;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Identity.Entities;
using SignUpOtpEntity = BackendProjectTemplate.Domain.Identity.Entities.SignUpOtp;

namespace BackendProjectTemplate.Application.Identity.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IRepository<AppUser> users,
    IRepository<SignUpOtpEntity> signUpOtps,
    IUnitOfWork unitOfWork,
    IOtpCodeService otpCodeService,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = AppUser.NormalizeEmail(request.Email);
        var otpHash = otpCodeService.HashCode(normalizedEmail, request.Otp);
        var now = timeProvider.GetUtcNow();

        var otp = await signUpOtps.FirstOrDefaultAsync(
            new SignUpOtpByEmailAndHashSpecification(normalizedEmail, otpHash),
            cancellationToken);

        if (otp is null || !otp.IsAvailable(now))
        {
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        var user = await users.FirstOrDefaultAsync(
            new UserByNormalizedEmailSpecification(normalizedEmail, tracked: true),
            cancellationToken);

        if (user is null)
        {
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        if (user.IsEmailVerified)
        {
            return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
        }

        otp.MarkConsumed(now);
        user.MarkEmailVerified(now);

        signUpOtps.Update(otp);
        users.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SignUpOtpResult(SignUpOtpStatus.Success);
    }
}

public sealed record SignUpOtpResult(SignUpOtpStatus Status);

public enum SignUpOtpStatus
{
    Success = 1,
    InvalidOtp = 2,
    AlreadyVerified = 3
}
