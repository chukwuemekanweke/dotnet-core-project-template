using BackendProjectTemplate.Application.Kyc.Features.ReconcileVerifications;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Jobs.Kyc;

public sealed class KycReconciliationProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KycReconciliationOptions> options,
    BackgroundServiceReadinessState readinessState,
    TimeProvider timeProvider,
    ILogger<KycReconciliationProcessor> logger) : BackgroundService
{
    public const string ServiceName = nameof(KycReconciliationProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<KycReconciliationService>();
                var now = timeProvider.GetUtcNow();

                var result = await service.HandleAsync(
                    now - options.Value.StaleThreshold,
                    now - options.Value.RecheckDelay,
                    options.Value.BatchSize,
                    stoppingToken);
                if (result.FailedCount > 0)
                {
                    logger.LogWarning(
                        "KYC reconciliation checked {CheckedCount} verifications; {FailedCount} could not be fetched from the provider.",
                        result.CheckedCount,
                        result.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "KYC reconciliation iteration failed.");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }
    }
}
