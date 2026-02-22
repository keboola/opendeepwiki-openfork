using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Execution;

/// <summary>
/// Agent response
/// </summary>
/// <param name="Success">Whether successful</param>
/// <param name="Messages">Response message list</param>
/// <param name="ErrorMessage">Error message (if failed)</param>
public record AgentResponse(
    bool Success,
    IEnumerable<IChatMessage> Messages,
    string? ErrorMessage = null
)
{
    /// <summary>
    /// Create a success response
    /// </summary>
    public static AgentResponse CreateSuccess(IEnumerable<IChatMessage> messages)
        => new(true, messages);
    
    /// <summary>
    /// Create a success response (single message)
    /// </summary>
    public static AgentResponse CreateSuccess(IChatMessage message)
        => new(true, new[] { message });
    
    /// <summary>
    /// Create a failure response
    /// </summary>
    public static AgentResponse CreateFailure(string errorMessage)
        => new(false, Enumerable.Empty<IChatMessage>(), errorMessage);
}

/// <summary>
/// Agent response chunk (streaming)
/// </summary>
/// <param name="Content">Content chunk</param>
/// <param name="IsComplete">Whether complete</param>
/// <param name="ErrorMessage">Error message (if any)</param>
public record AgentResponseChunk(
    string Content,
    bool IsComplete,
    string? ErrorMessage = null
)
{
    /// <summary>
    /// Create a content chunk
    /// </summary>
    public static AgentResponseChunk CreateContent(string content)
        => new(content, false);
    
    /// <summary>
    /// Create a completion chunk
    /// </summary>
    public static AgentResponseChunk CreateComplete()
        => new(string.Empty, true);
    
    /// <summary>
    /// Create an error chunk
    /// </summary>
    public static AgentResponseChunk CreateError(string errorMessage)
        => new(string.Empty, true, errorMessage);
}
