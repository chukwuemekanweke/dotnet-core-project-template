using BackendProjectTemplate.Consumer.UnitTests.Payments;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments.CreditWallet;

public sealed class When_HandlingCreditWallet_WithNewWallet_Should
{
    [Fact]
    public async Task CreditWallet()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var currencyId = Guid.CreateVersion7();
        var command = context.CreateCreditWalletCommand(2500m, currencyId);
        Wallet? capturedWallet = null;
        WalletTransaction? capturedTransaction = null;

        context.WalletTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<WalletTransaction>>(), Arg.Any<CancellationToken>())
            .Returns((WalletTransaction?)null);
        context.WalletRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Wallet>>(), Arg.Any<CancellationToken>())
            .Returns((Wallet?)null);
        context.WalletRepository.AddAsync(Arg.Do<Wallet>(wallet => capturedWallet = wallet), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.WalletTransactionRepository.AddAsync(Arg.Do<WalletTransaction>(transaction => capturedTransaction = transaction), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await context.CreateCreditWalletHandler().HandleAsync(command, CancellationToken.None);

        capturedWallet.ShouldNotBeNull();
        capturedWallet.Balance.ShouldBe(2500m);
        capturedTransaction.ShouldNotBeNull();
        capturedTransaction.PaymentTransactionId.ShouldBe(command.PaymentTransactionId);
        capturedTransaction.Amount.ShouldBe(2500m);
        capturedTransaction.TransactionType.ShouldBe(WalletTransactionType.Credit);
        capturedTransaction.TransactionCategory.ShouldBe(WalletTransactionCategory.WalletFunding);
        capturedTransaction.TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        context.WalletRepository.DidNotReceive().Update(Arg.Any<Wallet>());
        context.CustomTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Payments.ValueGranted,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.StepNamePropertyName] == Observability.StepNames.ValueGrant &&
                properties[Observability.OutcomePropertyName] == Observability.Outcomes.Success &&
                properties[Observability.PaymentReferencePropertyName] == command.MerchantReference));
    }
}
