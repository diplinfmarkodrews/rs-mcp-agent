using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using ReportServerPort;
using ReportServerRPCClient.Infrastructure;
using ReportServerRPCClient.Services;

namespace ReportServerRPCClient.Extensions;

public static class HostExtension
{
    public static IServiceCollection AddReportServerRpcClient(
        this IServiceCollection services,
        string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
        
        services.AddTransient<CookieAccessibleHttpMessageHandler>();
        services.AddHttpClient("ReportServerGwtRpcClient", client => 
            {
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Add("Content-Type", "text/x-gwt-rpc; charset=UTF-8");
                client.DefaultRequestHeaders.Add("X-GWT-Module-Base", $"{baseUrl.TrimEnd('/')}/reportserver/");
                client.DefaultRequestHeaders.Add("X-GWT-Permutation", "strongName");
            })
            .AddHttpMessageHandler<CookieAccessibleHttpMessageHandler>()
            .AddTransientHttpErrorPolicy(config => 
                config.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddScoped<IReportServerClient, ReportServerGwtRpcClient>();
        return services;
    }
}