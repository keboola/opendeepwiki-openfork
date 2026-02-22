using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenDeepWiki.Chat.Providers;
using OpenDeepWiki.Chat.Routing;

namespace OpenDeepWiki.Chat;

/// <summary>
/// Provider initialization background service
/// Responsible for initializing all providers and registering them with the router on application startup
/// Requirements: 2.2, 2.4
/// </summary>
public class ProviderInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderInitializationService> _logger;

    public ProviderInitializationService(
        IServiceProvider serviceProvider,
        ILogger<ProviderInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Chat Provider initialization...");

        using var scope = _serviceProvider.CreateScope();
        var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageProvider>>();
        var router = _serviceProvider.GetRequiredService<IMessageRouter>();

        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation("Initializing Provider: {PlatformId} ({DisplayName})", 
                    provider.PlatformId, provider.DisplayName);

                await provider.InitializeAsync(cancellationToken);
                
                // Register with the router
                router.RegisterProvider(provider);
                
                _logger.LogInformation("Provider {PlatformId} initialized successfully, enabled: {IsEnabled}", 
                    provider.PlatformId, provider.IsEnabled);
            }
            catch (Exception ex)
            {
                // Requirements: 2.4 - Log errors on Provider initialization failure and continue loading other providers
                _logger.LogError(ex, "Provider {PlatformId} initialization failed, will continue loading other providers", 
                    provider.PlatformId);
            }
        }

        _logger.LogInformation("Chat Provider initialization complete, {Count} providers registered", 
            router.GetAllProviders().Count());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down Chat Providers...");

        var router = _serviceProvider.GetRequiredService<IMessageRouter>();
        
        foreach (var provider in router.GetAllProviders())
        {
            try
            {
                await provider.ShutdownAsync(cancellationToken);
                _logger.LogInformation("Provider {PlatformId} has been shut down", provider.PlatformId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while shutting down Provider {PlatformId}", provider.PlatformId);
            }
        }

        _logger.LogInformation("All Chat Providers have been shut down");
    }
}
