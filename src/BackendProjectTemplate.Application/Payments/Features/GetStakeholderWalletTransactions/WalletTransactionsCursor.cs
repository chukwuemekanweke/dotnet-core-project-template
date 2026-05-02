using System.Text.Json;

namespace BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;

internal static class WalletTransactionsCursor
{
    public static (DateTimeOffset? CreatedAtUtc, Guid? TransactionId) Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var payload = JsonSerializer.Deserialize<WalletTransactionsCursorPayload>(bytes);
            if (payload is null || payload.TransactionId == Guid.Empty)
            {
                throw new InvalidOperationException("Invalid wallet transactions cursor.");
            }

            return (DateTimeOffset.FromUnixTimeMilliseconds(payload.CreatedAtUnixMilliseconds), payload.TransactionId);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException("Invalid wallet transactions cursor.", exception);
        }
    }

    public static string Encode(DateTimeOffset createdAtUtc, Guid transactionId)
    {
        var payload = new WalletTransactionsCursorPayload(createdAtUtc.ToUnixTimeMilliseconds(), transactionId);
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        return Convert.ToBase64String(json);
    }

    private sealed record WalletTransactionsCursorPayload(long CreatedAtUnixMilliseconds, Guid TransactionId);
}
