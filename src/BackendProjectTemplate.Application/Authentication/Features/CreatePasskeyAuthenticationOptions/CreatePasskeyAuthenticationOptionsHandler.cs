using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyAuthenticationOptions;

public sealed class CreatePasskeyAuthenticationOptionsHandler(IPasskeyService passkeyService)
{
    public Task<PasskeyOptions> HandleAsync(
        CreatePasskeyAuthenticationOptionsCommand command,
        CancellationToken cancellationToken) =>
        passkeyService.CreateAuthenticationOptionsAsync(command.TenantId, cancellationToken);
}
