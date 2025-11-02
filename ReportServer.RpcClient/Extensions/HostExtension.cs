using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using ReportServer.Abstraction;
using ReportServer.RpcClient.Infrastructure;
using ReportServer.RpcClient.Services;

namespace ReportServer.RpcClient.Extensions;

public static class HostExtension
{
    public static IServiceCollection AddReportServerRpcClient(
        this IServiceCollection services,
        string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
        
        services.AddSingleton<CookieContainerProvider>();
        // services.AddSingleton<CookieAccessibleHttpClientHandler>();
        services.AddHttpClient("ReportServerGwtRpcClient", client => 
            {
                // BaseAddress MUST end with a slash for proper relative URL resolution
                client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");
                // client.DefaultRequestHeaders.Add("Content-Type", "text/x-gwt-rpc; charset=UTF-8");
                client.DefaultRequestHeaders.Add("X-GWT-Module-Base", $"{baseUrl.TrimEnd('/')}/reportserver/");
                // GWT Permutation Hash - extracted from actual ReportServer traffic
                // This hash identifies the specific compiled JavaScript permutation
                client.DefaultRequestHeaders.Add("X-GWT-Permutation", "0960CCE3B17B0C25D12B6D12FA467931");
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var cookieProvider = provider.GetRequiredService<CookieContainerProvider>();
                return new HttpClientHandler
                {
                    CookieContainer = cookieProvider.CookieContainer,
                    UseCookies = true,
                };
            })
            .AddTransientHttpErrorPolicy(config => 
                config.WaitAndRetryAsync(3, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddScoped<IReportServerClient, ReportServerGwtRpcClient>();
        return services;
    }
}


