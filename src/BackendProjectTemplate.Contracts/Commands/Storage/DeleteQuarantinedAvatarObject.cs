namespace BackendProjectTemplate.Contracts.Commands.Storage;

public sealed record DeleteQuarantinedAvatarObject(
    Guid UploadId,
    string ObjectKey) : BaseCommand;
