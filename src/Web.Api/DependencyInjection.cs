using System.Globalization;
using System.Threading.RateLimiting;
using Web.Api.Infrastructure;

namespace Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // REMARK: If you want to use Controllers, you'll need this.
        services.AddControllers();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddRateLimiting();
        services.AddApiVersioning();

        return services;
    }

    private static IServiceCollection AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.QueryStringApiVersionReader("api-version"),
                new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
                new Asp.Versioning.UrlSegmentApiVersionReader());
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    private static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter: 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });

            // Auth endpoints policy: 5 requests per minute (brute-force protection)
            options.AddPolicy("auth", context =>
            {
                string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4
                    });
            });

            // Authenticated users policy: 1000 requests per hour
            options.AddPolicy("authenticated", context =>
            {
                string userId = context.User.FindFirst("sub")?.Value
                    ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: userId,
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 1000,
                        ReplenishmentPeriod = TimeSpan.FromHours(1),
                        TokensPerPeriod = 1000,
                        AutoReplenishment = true
                    });
            });

            // Custom rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "TooManyRequests",
                    message = "Rate limit exceeded. Please retry after some time.",
                    retryAfter = retryAfter.TotalSeconds
                }, cancellationToken: cancellationToken);
            };
        });

        return services;
    }
}
