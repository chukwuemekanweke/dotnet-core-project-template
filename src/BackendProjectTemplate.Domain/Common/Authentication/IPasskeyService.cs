using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IPasskeyService
{
    Task<PasskeyOptions> CreateRegistrationOptionsAsync(
        AppUser user,
        Guid stakeholderId,
        Guid tenantId,
        string passkeyName,
        CancellationToken cancellationToken);
    Task<PasskeyRegistrationResult> CompleteRegistrationAsync(
        AppUser user,
        Guid stakeholderId,
        Guid tenantId,
        string flowToken,
        string credentialJson,
        CancellationToken cancellationToken);
    Task<PasskeyOptions> CreateAuthenticationOptionsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<PasskeyAuthenticationResult> VerifyAuthenticationAsync(
        Guid tenantId,
        string flowToken,
        string credentialJson,
        CancellationToken cancellationToken);
    Task<PasskeyOperationStatus> UpdateAuthenticationStateAsync(AppUser user, UserPasskeyInfo passkey);
    Task<IReadOnlyList<PasskeyCredential>> ListAsync(AppUser user);
    Task<PasskeyOperationStatus> RenameAsync(AppUser user, string credentialId, string name);
    Task<PasskeyOperationStatus> RemoveAsync(AppUser user, string credentialId);
}
