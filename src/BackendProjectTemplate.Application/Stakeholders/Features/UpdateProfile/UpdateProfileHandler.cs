using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;

public sealed class UpdateProfileHandler(
    ICurrentActor currentActor,
    IRepository<Stakeholder> stakeholderRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<UpdateProfileResult> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            return new UpdateProfileResult(UpdateProfileStatus.NotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
        {
            return new UpdateProfileResult(
                UpdateProfileStatus.ValidationFailed,
                "FirstName and LastName are required.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null)
        {
            return new UpdateProfileResult(UpdateProfileStatus.StakeholderNotFound);
        }

        stakeholder.UpdateProfile(command.FirstName, command.LastName, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResult(UpdateProfileStatus.Success);
    }
}
