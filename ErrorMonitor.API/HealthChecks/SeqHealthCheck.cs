using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ErrorMonitor.API.HealthChecks;

/// <summary>
/// يفحص الاتصال بخادم Seq للتأكد من أن التسجيل المركزي يعمل
/// </summary>
public class SeqHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var seqUrl = configuration["SeqSettings:ServerUrl"] ?? "http://localhost:5341";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            // Seq يوفر endpoint /health للتحقق من صحته
            var response = await client.GetAsync($"{seqUrl}/health", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Seq is reachable at {seqUrl}")
                : HealthCheckResult.Degraded($"Seq returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Cannot reach Seq at {seqUrl}", ex);
        }
    }
}
