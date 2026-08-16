using BackendProjectTemplate.Application.Common.FileUploads;
using BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;
using BackendProjectTemplate.Application.Stakeholders.Features.CreateAvatarUpload;
using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

internal static class ProfilesControllerTestFactory
{
    public static ProfilesController Create(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        IRepository<Stakeholder> stakeholderRepository,
        ICustomTelemetryContext customTelemetryContext,
        IUnitOfWork unitOfWork,
        ICurrentActor currentActor,
        IObjectStorageService? objectStorageService = null,
        IRepository<AvatarUpload>? avatarUploadRepository = null,
        ILogger<ProfilesController>? logger = null)
    {
        objectStorageService ??= Substitute.For<IObjectStorageService>();
        avatarUploadRepository ??= Substitute.For<IRepository<AvatarUpload>>();

        return new ProfilesController(
            new GetProfileHandler(stakeholderReadModelRepository),
            new CreateAvatarUploadHandler(
                stakeholderRepository,
                avatarUploadRepository,
                new FileUploadService(objectStorageService),
                customTelemetryContext,
                unitOfWork,
                TimeProvider.System),
            new CompleteAvatarUploadHandler(
                stakeholderRepository,
                avatarUploadRepository,
                new FileUploadService(objectStorageService),
                Substitute.For<ICommandSender>(),
                customTelemetryContext,
                unitOfWork,
                TimeProvider.System),
            new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork),
            currentActor,
            logger ?? NullLogger<ProfilesController>.Instance);
    }
}
