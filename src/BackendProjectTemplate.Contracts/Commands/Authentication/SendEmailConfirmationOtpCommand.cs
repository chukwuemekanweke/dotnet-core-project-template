namespace BackendProjectTemplate.Contracts.Commands.Authentication;

public sealed record SendEmailConfirmationOtpCommand : BaseCommand
{
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
