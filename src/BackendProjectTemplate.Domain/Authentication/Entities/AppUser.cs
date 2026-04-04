using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class AppUser : Entity
{
    private readonly List<SignUpOtp> _signUpOtps = [];

    private AppUser()
    {
    }

    private AppUser(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        string passwordSalt,
        DateTimeOffset utcNow)
    {
        Email = email.Trim();
        NormalizedEmail = NormalizeEmail(email);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        SetAuditDates(utcNow);
    }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public bool IsEmailVerified { get; private set; }
    public IReadOnlyCollection<SignUpOtp> SignUpOtps => _signUpOtps;

    public static AppUser Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        string passwordSalt,
        DateTimeOffset utcNow) =>
        new(email, firstName, lastName, passwordHash, passwordSalt, utcNow);

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public void MarkEmailVerified(DateTimeOffset utcNow)
    {
        IsEmailVerified = true;
        Touch(utcNow);
    }
}
