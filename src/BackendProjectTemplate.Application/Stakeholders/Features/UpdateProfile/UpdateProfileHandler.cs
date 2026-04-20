using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Persistence;

namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;

public sealed class UpdateProfileHandler(
    ICurrentActor currentActor,
    IAppUserStakeholderRepository appUserStakeholderRepository,
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

        var appUserStakeholder = await appUserStakeholderRepository.GetByStakeholderIdAsync(stakeholderId, cancellationToken);
        if (appUserStakeholder is null)
        {
            return new UpdateProfileResult(UpdateProfileStatus.StakeholderNotFound);
        }

        appUserStakeholder.Stakeholder.UpdateProfile(command.FirstName, command.LastName, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResult(UpdateProfileStatus.Success);
    }
}
