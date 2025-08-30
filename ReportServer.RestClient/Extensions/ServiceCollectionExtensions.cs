using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ReportServer.Abstraction;
using ReportServer.RestClient.Infrastructure;
using ReportServer.RestClient.Services;
using ReportServer.RestClient.Mapper;

namespace ReportServer.RestClient.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportServerRestClient(this IServiceCollection services, string baseUrl, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        // Add AutoMapper
        services.AddAutoMapper(typeof(RestClientMappingProfile));
        services.AddScoped<CookieContainerProvider>();
        // Configure HttpClient with retry policy
        services.AddHttpClient<IReportServerClient, RsRestClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = timeout ?? TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "ReportServer-RestClient/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy());

        

        return services;
    }

    public static IServiceCollection AddReportServerRestClient(this IServiceCollection services, Action<HttpClient> configureClient)
    {
        if (configureClient == null)
            throw new ArgumentNullException(nameof(configureClient));

        services.AddScoped<CookieContainerProvider>();
        // Add AutoMapper
        services.AddAutoMapper(typeof(RestClientMappingProfile));

        // Configure HttpClient with custom configuration and retry policy
        services.AddHttpClient("ReportServerRestClient", configureClient)
            .AddPolicyHandler(GetRetryPolicy());

        // Register the client as IReportServerClient
        services.AddScoped<IReportServerClient, RsRestClient>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        context.TryGetValue("logger", out var loggerObj);
                        var logger = loggerObj as ILogger;
                        logger?.LogWarning("Retry {RetryCount} for {OperationKey} in {Delay}ms due to: {Exception}",
                            retryCount, context.OperationKey, timespan.TotalMilliseconds, outcome.Exception.Message);
                    }
                    else
                    {
                        context.TryGetValue("logger", out var loggerObj);
                        var logger = loggerObj as ILogger;
                        logger?.LogWarning("Retry {RetryCount} for {OperationKey} in {Delay}ms due to: {StatusCode}",
                            retryCount, context.OperationKey, timespan.TotalMilliseconds, outcome.Result.StatusCode);
                    }
                });
    }
}
