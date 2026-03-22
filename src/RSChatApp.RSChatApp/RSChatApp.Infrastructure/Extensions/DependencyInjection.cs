using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Application.Services;
using RSChatApp.Infrastructure.EventStore;
using RSChatApp.Infrastructure.Identity.Clients;
using RSChatApp.Infrastructure.Identity.Services;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Infrastructure.ReportServer.Clients;
using Wolverine.Marten;

namespace RSChatApp.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add infrastructure services here
        // Identity
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationClient, LegacyAuthenticationClient>();
        services.AddScoped<IRsTerminalClient, RsTerminalClient>();
        services.AddScoped<ILegacyAuthenticationService, LegacyAuthenticationService>();
        
        services.ConfigureWolverineMarten(configuration);
        
        return services;
    
    }
    
    public static IServiceCollection AddPromptServices(this IServiceCollection services)
    {
        services.AddSingleton<IPromptFileStore, PromptFileStore>();
        services.AddScoped<IPromptService, PromptService>();
        services.AddHostedService<PromptStartupValidatorHostedService>();
        return services;
    }
    public static void ConfigureWolverineMarten(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("postgres")
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured.");
        
        services.AddMarten(sp =>
        {
            var opts = new StoreOptions();

            opts.Connection(connectionString);

            // Marten event types
            opts.Events.AddEventType<SessionCreatedEvent>();
            opts.Events.AddEventType<MessageCreatedEvent>();
            opts.Events.AddEventType<SessionUpdatedEvent>();
            opts.Events.AddEventType<SessionDeletedEvent>();

            // Inline projections (synchronous, no daemon needed)
            opts.Projections.Add<ConversationProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<MessageProjection>(ProjectionLifecycle.Inline);

            return opts;
        })
        // HotCold: required for PublishEventsToWolverine subscriptions
        .AddAsyncDaemon(DaemonMode.HotCold)
        // Wolverine/Marten integration: saga persistence + outbox + event forwarding
        .IntegrateWithWolverine();

        // Marten sessions for DI
        services.AddScoped<IDocumentSession>(sp =>
            sp.GetRequiredService<IDocumentStore>()
                .LightweightSession());
        
        services.AddScoped<IQuerySession>(sp =>
            sp.GetRequiredService<IDocumentStore>()
                .QuerySession());

        // Event store abstractions
        services.AddScoped(typeof(IEventStoreRepository<>), typeof(MartenEventStoreRepository<>));
        services.AddScoped<IReadOnlyEventStore, MartenReadOnlyEventStore>();
    }
    
}