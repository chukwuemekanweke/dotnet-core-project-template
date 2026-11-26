namespace BackendProjectTemplate.WebAPI.Features.Payments.Providers;

/// <summary>The optional reason is recorded in the administrative audit journal.</summary>
public sealed record SetPaymentProviderActivationRequest(bool IsActive, string? Reason = null);
