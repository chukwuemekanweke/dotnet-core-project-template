using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequestRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        Validate(options.AuthPublicPolicy, nameof(RateLimitingOptions.AuthPublicPolicy));
        Validate(options.AuthenticatedUserPolicy, nameof(RateLimitingOptions.AuthenticatedUserPolicy));
        Validate(options.GlobalFallbackPolicy, nameof(RateLimitingOptions.GlobalFallbackPolicy));

        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.GlobalLimiter = CreatePartitionedLimiter(options.GlobalFallbackPolicy, ResolveGlobalFallbackPartitionKey);

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.AuthPublicPolicy,
                context => CreateFixedWindowPartition(context, options.AuthPublicPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.AuthenticatedUserPolicy,
                context => CreateFixedWindowPartition(context, options.AuthenticatedUserPolicy, ResolveAuthenticatedUserPartitionKey(context)));

            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue
                    : null;

                if (retryAfter is not null)
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.Value.TotalSeconds).ToString("F0");
                }

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Too many requests were sent in a short period. Please wait and try again."
                    }
                });
            };
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreatePartitionedLimiter(
        RateLimitingOptions.PolicyOptions policy,
        Func<HttpContext, string> partitionResolver)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(
            context => CreateFixedWindowPartition(context, policy, partitionResolver(context)));
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        RateLimitingOptions.PolicyOptions policy,
        string partitionKey)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                QueueLimit = policy.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string ResolveAuthenticatedUserPartitionKey(HttpContext context)
    {
        var appUserId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return Guid.TryParse(appUserId, out var parsedAppUserId)
            ? $"user:{parsedAppUserId:D}"
            : ResolveClientIpPartitionKey(context);
    }

    private static string ResolveGlobalFallbackPartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return ResolveAuthenticatedUserPartitionKey(context);
        }

        return ResolveClientIpPartitionKey(context);
    }

    private static string ResolveClientIpPartitionKey(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return "ip:unknown";
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return $"ip:{remoteIpAddress}";
    }

    private static void Validate(RateLimitingOptions.PolicyOptions policy, string name)
    {
        if (policy.PermitLimit <= 0)
        {
            throw new InvalidOperationException($"{name} permit limit must be greater than zero.");
        }

        if (policy.WindowMinutes <= 0)
        {
            throw new InvalidOperationException($"{name} window minutes must be greater than zero.");
        }

        if (policy.QueueLimit < 0)
        {
            throw new InvalidOperationException($"{name} queue limit must be zero or greater.");
        }
    }
}
