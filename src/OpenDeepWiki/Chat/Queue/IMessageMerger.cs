using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Message merger interface
/// Used to merge multiple short messages into one
/// </summary>
public interface IMessageMerger
{
    /// <summary>
    /// Try to merge messages
    /// </summary>
    /// <param name="messages">List of messages to merge</param>
    /// <returns>Merge result; returns original messages if merging is not possible</returns>
    MergeResult TryMerge(IReadOnlyList<IChatMessage> messages);
    
    /// <summary>
    /// Check whether messages can be merged
    /// </summary>
    /// <param name="messages">List of messages to check</param>
    /// <returns>Whether the messages can be merged</returns>
    bool CanMerge(IReadOnlyList<IChatMessage> messages);
}

/// <summary>
/// Merge result
/// </summary>
/// <param name="WasMerged">Whether merging was performed</param>
/// <param name="Messages">Result message list</param>
public record MergeResult(bool WasMerged, IReadOnlyList<IChatMessage> Messages);
