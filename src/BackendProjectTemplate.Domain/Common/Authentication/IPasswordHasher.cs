namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool Verify(string password, string expectedHash, string storedSalt);
}
