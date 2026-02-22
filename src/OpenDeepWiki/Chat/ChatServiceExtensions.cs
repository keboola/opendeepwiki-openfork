using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenDeepWiki.Chat.Callbacks;
using OpenDeepWiki.Chat.Config;
using OpenDeepWiki.Chat.Execution;
using OpenDeepWiki.Chat.Processing;
using OpenDeepWiki.Chat.Providers;
using OpenDeepWiki.Chat.Providers.Feishu;
using OpenDeepWiki.Chat.Providers.QQ;
using OpenDeepWiki.Chat.Providers.Slack;
using OpenDeepWiki.Chat.Providers.WeChat;
using OpenDeepWiki.Chat.Queue;
using OpenDeepWiki.Chat.Routing;
using OpenDeepWiki.Chat.Sessions;

namespace OpenDeepWiki.Chat;

/// <summary>
/// Chat service registration extension methods
/// Requirements: 2.2, 2.4
/// </summary>
public static class ChatServiceExtensions
{
    /// <summary>
    /// Add all Chat system services
    /// </summary>
    public static IServiceCollection AddChatServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        services.AddChatOptions(configuration);

        // Register core services
        services.AddChatCoreServices();

        // Register providers
        services.AddChatProviders(configuration);

        // Register background services
        services.AddChatBackgroundServices(configuration);

        // Register configuration validation startup filter
        services.AddChatStartupValidation(configuration);
        
        return services;
    }
    
    /// <summary>
    /// Register configuration options
    /// </summary>
    private static IServiceCollection AddChatOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Chat global configuration
        services.Configure<ChatConfigOptions>(configuration.GetSection(ChatConfigOptions.SectionName));

        // Configuration encryption options
        services.Configure<ConfigEncryptionOptions>(configuration.GetSection("Chat:Encryption"));

        // Session management configuration
        services.Configure<SessionManagerOptions>(configuration.GetSection(SessionManagerOptions.SectionName));

        // Message queue configuration
        services.Configure<MessageQueueOptions>(configuration.GetSection(MessageQueueOptions.SectionName));

        // Agent executor configuration
        services.Configure<AgentExecutorOptions>(configuration.GetSection("Chat:AgentExecutor"));

        // Message processing configuration
        services.Configure<ChatProcessingOptions>(configuration.GetSection(ChatProcessingOptions.SectionName));

        // Provider configuration
        services.Configure<FeishuProviderOptions>(configuration.GetSection("Chat:Providers:Feishu"));
        services.Configure<QQProviderOptions>(configuration.GetSection("Chat:Providers:QQ"));
        services.Configure<WeChatProviderOptions>(configuration.GetSection("Chat:Providers:WeChat"));
        services.Configure<SlackProviderOptions>(configuration.GetSection("Chat:Providers:Slack"));
        
        return services;
    }
    
    /// <summary>
    /// Register core services
    /// </summary>
    private static IServiceCollection AddChatCoreServices(this IServiceCollection services)
    {
        // Configuration encryption service
        services.TryAddSingleton<IConfigEncryption, AesConfigEncryption>();

        // Configuration management service
        services.TryAddScoped<IChatConfigService, ChatConfigService>();

        // Session management service
        services.TryAddScoped<ISessionManager, SessionManager>();

        // Message queue service
        services.TryAddScoped<IMessageQueue, DatabaseMessageQueue>();

        // Message merger
        services.TryAddSingleton<IMessageMerger, TextMessageMerger>();
        
        // User identity resolver (Singleton with in-memory cache)
        services.AddHttpClient<ChatUserResolver>();
        services.TryAddSingleton<IChatUserResolver, ChatUserResolver>();

        // Agent executor
        services.TryAddScoped<IAgentExecutor, AgentExecutor>();

        // Message router (Singleton, because it needs to maintain the Provider registry)
        // Uses IServiceScopeFactory to resolve scoped deps (SessionManager, MessageQueue, etc.)
        services.TryAddSingleton<IMessageRouter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MessageRouter>>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new MessageRouter(logger, scopeFactory);
        });
        
        // Message callback manager
        services.TryAddScoped<IMessageCallback>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CallbackManager>>();
            var messageQueue = sp.GetRequiredService<IMessageQueue>();
            var router = sp.GetRequiredService<IMessageRouter>();
            
            return new CallbackManager(
                logger,
                messageQueue,
                platform => router.GetProvider(platform));
        });
        
        // Configuration change notifier (Singleton)
        services.TryAddSingleton<IConfigChangeNotifier, ConfigChangeNotifier>();
        
        return services;
    }
    
    /// <summary>
    /// Register providers
    /// Requirements: 2.2 - Automatically discover and load via dependency injection
    /// </summary>
    private static IServiceCollection AddChatProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HttpClient factory
        services.AddHttpClient<FeishuProvider>();
        services.AddHttpClient<QQProvider>();
        services.AddHttpClient<WeChatProvider>();
        services.AddHttpClient<SlackProvider>();
        
        // Register providers as Scoped services
        services.TryAddScoped<FeishuProvider>();
        services.TryAddScoped<QQProvider>();
        services.TryAddScoped<WeChatProvider>();
        services.TryAddScoped<SlackProvider>();
        
        // Register provider collection (for auto-discovery)
        services.TryAddScoped<IEnumerable<IMessageProvider>>(sp => new IMessageProvider[]
        {
            sp.GetRequiredService<FeishuProvider>(),
            sp.GetRequiredService<QQProvider>(),
            sp.GetRequiredService<WeChatProvider>(),
            sp.GetRequiredService<SlackProvider>()
        });
        
        // Register provider initialization service
        services.AddHostedService<ProviderInitializationService>();
        
        return services;
    }
    
    /// <summary>
    /// Register background services
    /// </summary>
    private static IServiceCollection AddChatBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        var processingOptions = configuration.GetSection(ChatProcessingOptions.SectionName).Get<ChatProcessingOptions>();
        
        // Only register message processing Worker when enabled
        if (processingOptions?.Enabled ?? true)
        {
            services.AddHostedService<ChatMessageProcessingWorker>();
        }
        
        // Configuration hot-reload service
        var chatOptions = configuration.GetSection(ChatConfigOptions.SectionName).Get<ChatConfigOptions>();
        if (chatOptions?.EnableHotReload ?? true)
        {
            services.AddHostedService<ConfigReloadService>();
        }

        // Apply DB config to providers after they are initialized
        services.AddHostedService<ProviderConfigApplicator>();

        return services;
    }
    
    /// <summary>
    /// Register startup validation
    /// </summary>
    private static IServiceCollection AddChatStartupValidation(this IServiceCollection services, IConfiguration configuration)
    {
        var chatOptions = configuration.GetSection(ChatConfigOptions.SectionName).Get<ChatConfigOptions>();
        
        // Only register configuration validation startup filter when enabled
        if (chatOptions?.ValidateOnStartup ?? true)
        {
            services.AddTransient<IStartupFilter, ConfigValidationStartupFilter>();
        }
        
        return services;
    }
}
