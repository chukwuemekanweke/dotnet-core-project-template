using BackendProjectTemplate.Application.Common.FileUploads;
using BackendProjectTemplate.Domain.Common.Storage;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Common.FileUploads;

public sealed class When_PreparingFileUpload_WithModulePolicy_Should
{
    [Fact]
    public void ApplyModuleRulesAndBuildModuleKeys()
    {
        var tenantId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var utcNow = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
        var service = new FileUploadService(Substitute.For<IObjectStorageService>());

        var result = service.Prepare(
            new FileUploadPreparationRequest(
                tenantId,
                ownerId,
                " agreement.pdf ",
                " APPLICATION/PDF ",
                1024),
            new TestDocumentUploadPolicy(),
            utcNow);

        result.IsValid.ShouldBeTrue();
        result.OriginalFileName.ShouldBe("agreement.pdf");
        result.ContentType.ShouldBe("application/pdf");
        result.FileExtension.ShouldBe(".pdf");
        result.QuarantineObjectKey.ShouldStartWith($"quarantine/documents/tenants/{tenantId}/owners/{ownerId}/");
        result.FinalObjectKey.ShouldStartWith($"tenants/{tenantId}/owners/{ownerId}/documents/");
        result.ExpiresAtUtc.ShouldBe(utcNow.AddMinutes(20));
    }
}
