namespace BackendProjectTemplate.Domain.Kyc;

public interface IKycCallbackSignatureValidator
{
    bool IsValid(ReadOnlySpan<byte> rawPayload, string? signature);
}
