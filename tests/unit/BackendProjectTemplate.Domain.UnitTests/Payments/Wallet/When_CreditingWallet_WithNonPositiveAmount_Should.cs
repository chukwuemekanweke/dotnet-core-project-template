using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Payments.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.Wallets;

public sealed class When_CreditingWallet_WithNonPositiveAmount_Should
{
    [Fact]
    public void ThrowAggregateStateException()
    {
        var wallet = Wallet.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var exception = Should.Throw<AggregateStateException>(() => wallet.Credit(0m));

        exception.Message.ShouldContain("greater than zero");
    }
}
