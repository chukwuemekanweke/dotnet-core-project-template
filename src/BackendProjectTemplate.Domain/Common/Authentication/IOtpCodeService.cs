namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IOtpCodeService
{
    string GenerateCode();
    string HashCode(string normalizedEmail, string code);
}
