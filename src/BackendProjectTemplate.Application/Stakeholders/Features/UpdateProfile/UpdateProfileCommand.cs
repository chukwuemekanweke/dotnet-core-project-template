namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    ActorContext ActorContext);
