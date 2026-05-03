using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_ValidatingMailtrapWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnInvalidStatus()
    {
        var sut = new MailtrapWebhookSignatureValidator(
            Options.Create(new EmailNotificationsOptions
            {
                Mailtrap = new EmailNotificationsOptions.MailtrapOptions
                {
                    WebhookSigningSecret = "test-signing-secret"
                }
            }));

        var result = await sut.ValidateAsync(
            new MailtrapWebhookSignatureValidationRequest("invalid-signature", "{\"events\":[]}"),
            CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
    }
}
