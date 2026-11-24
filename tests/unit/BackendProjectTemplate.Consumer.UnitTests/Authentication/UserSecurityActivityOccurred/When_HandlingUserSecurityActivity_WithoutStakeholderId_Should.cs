using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication.UserSecurityActivityOccurred;

public sealed class When_HandlingUserSecurityActivity_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var context = new UserSecurityActivityOccurredTestContext();
        var message = UserSecurityActivityOccurredTestContext.CreateMessage(UserSecurityActivityTypes.PasswordChanged, null);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            context.CreateHandler().HandleAsync(message, CancellationToken.None));

        exception.Message.ShouldContain("stakeholder actor id", Case.Insensitive);
        await context.SecurityActivityRepository.DidNotReceive().AddAsync(Arg.Any<SecurityActivity>(), Arg.Any<CancellationToken>());
    }
}
