using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Providers;

namespace OpenDeepWiki.Chat.Routing;

/// <summary>
/// Message router interface
/// Responsible for routing messages to the correct Provider
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Route incoming message
    /// </summary>
    /// <param name="message">Incoming message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RouteIncomingAsync(IChatMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Route outgoing message
    /// </summary>
    /// <param name="message">Outgoing message</param>
    /// <param name="targetUserId">Target user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RouteOutgoingAsync(IChatMessage message, string targetUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the Provider for a specified platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Provider instance, or null if not found</returns>
    IMessageProvider? GetProvider(string platform);
    
    /// <summary>
    /// Get all registered Providers
    /// </summary>
    /// <returns>Collection of all registered Providers</returns>
    IEnumerable<IMessageProvider> GetAllProviders();
    
    /// <summary>
    /// Register a Provider
    /// </summary>
    /// <param name="provider">Provider to register</param>
    void RegisterProvider(IMessageProvider provider);
    
    /// <summary>
    /// Unregister a Provider
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Whether the unregistration was successful</returns>
    bool UnregisterProvider(string platform);
    
    /// <summary>
    /// Check whether a Provider is registered for the specified platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Whether it exists</returns>
    bool HasProvider(string platform);
}
