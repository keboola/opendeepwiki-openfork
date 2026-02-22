using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Sessions;

namespace OpenDeepWiki.Chat.Execution;

/// <summary>
/// Agent executor interface
/// Responsible for processing messages and generating responses
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Execute Agent to process a message
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="session">Chat session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent response</returns>
    Task<AgentResponse> ExecuteAsync(
        IChatMessage message, 
        IChatSession session, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute Agent in streaming mode
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="session">Chat session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of Agent response chunks</returns>
    IAsyncEnumerable<AgentResponseChunk> ExecuteStreamAsync(
        IChatMessage message, 
        IChatSession session, 
        CancellationToken cancellationToken = default);
}
