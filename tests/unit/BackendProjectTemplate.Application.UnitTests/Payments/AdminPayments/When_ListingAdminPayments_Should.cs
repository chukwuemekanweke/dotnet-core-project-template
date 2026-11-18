using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.AdminPayments;

public sealed class When_ListingAdminPayments_Should
{
    [Fact]
    public async Task PassNormalizedFiltersAndReturnDeterministicCursor()
    {
        var repository = Substitute.For<IPaymentAdministrationReadModelRepository>();
        var now = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
        var item = new AdminPaymentListItemReadModel(
            Guid.CreateVersion7(), "merchant-1", "provider-1", Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada Lovelace",
            Guid.CreateVersion7(), "Bachs", "bachs", 125000m, Guid.CreateVersion7(), "NGN", PaymentIntent.WalletTopUp,
            PaymentMethodType.Card, PaymentStatus.Succeeded, now, now, null);
        AdminPaymentListRequest? captured = null;
        repository.ListPaymentsAsync(Arg.Do<AdminPaymentListRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns(new AdminPaymentListPage([item], true));
        var priorCursor = CursorPagination.Encode(now.AddMinutes(1), Guid.CreateVersion7());

        var result = await new GetAdminPaymentsHandler(repository).HandleAsync(
            new GetAdminPaymentsCommand(
                item.TenantId, item.StakeholderId, item.PaymentProviderId, item.CurrencyId, item.PaymentIntent,
                item.PaymentStatus, item.PaymentMethodType, now.AddDays(-1), now.AddDays(1), "  merchant-1  ", priorCursor, 1),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.Search.ShouldBe("merchant-1");
        captured.CursorPaymentTransactionId.ShouldNotBeNull();
        captured.Limit.ShouldBe(1);
        result.Items.ShouldHaveSingleItem().PaymentStatus.ShouldBe("Succeeded");
        CursorPagination.Decode(result.NextCursor).EntityId.ShouldBe(item.TransactionId);
    }
}
