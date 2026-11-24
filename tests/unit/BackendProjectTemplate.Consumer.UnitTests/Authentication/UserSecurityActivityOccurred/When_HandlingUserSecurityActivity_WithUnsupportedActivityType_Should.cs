using BackendProjectTemplate.Domain.Authentication.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication.UserSecurityActivityOccurred;

public sealed class When_HandlingUserSecurityActivity_WithUnsupportedActivityType_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var context = new UserSecurityActivityOccurredTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var unsupportedActivityType = "sign_in";
        context.ArrangeStakeholder(stakeholderId);
        var message = UserSecurityActivityOccurredTestContext.CreateMessage(unsupportedActivityType, stakeholderId);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            context.CreateHandler().HandleAsync(message, CancellationToken.None));

        exception.Message.ShouldContain(unsupportedActivityType);
        await context.SecurityActivityRepository.DidNotReceive().AddAsync(Arg.Any<SecurityActivity>(), Arg.Any<CancellationToken>());
    }
}
