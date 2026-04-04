using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class AppUser : IdentityUser<Guid>
{
    private AppUser()
    {
    }

    private AppUser(string email, string firstName, string lastName, DateTimeOffset utcNow)
    {
        var normalizedEmail = email.Trim();

        UserName = normalizedEmail;
        Email = normalizedEmail;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static AppUser Create(string email, string firstName, string lastName, DateTimeOffset utcNow) =>
        new(email, firstName, lastName, utcNow);

    public void MarkEmailVerified(DateTimeOffset utcNow)
    {
        EmailConfirmed = true;
        UpdatedAtUtc = utcNow;
    }

    public void Touch(DateTimeOffset utcNow) => UpdatedAtUtc = utcNow;
}
