namespace BackendProjectTemplate.Contracts.Commands.Storage;

public sealed record DeleteQuarantinedObject(
    Guid UploadId,
    string ObjectKey) : BaseCommand;
