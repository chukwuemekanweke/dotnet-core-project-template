using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class OtpCodeService : IOtpCodeService
{
    public string GenerateCode() =>
        RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString(CultureInfo.InvariantCulture);

    public string HashCode(string normalizedEmail, string code)
    {
        var bytes = Encoding.UTF8.GetBytes($"{normalizedEmail}:{code}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
