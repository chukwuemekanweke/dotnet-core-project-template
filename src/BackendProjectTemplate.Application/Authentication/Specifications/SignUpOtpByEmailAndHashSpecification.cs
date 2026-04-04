using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Application.Authentication.Specifications;

public sealed class SignUpOtpByEmailAndHashSpecification : Specification<SignUpOtp>
{
    public SignUpOtpByEmailAndHashSpecification(string normalizedEmail, string codeHash)
    {
        Where(otp => otp.NormalizedEmail == normalizedEmail && otp.CodeHash == codeHash);
        AddInclude(otp => otp.User);
        EnableTracking();
    }
}
