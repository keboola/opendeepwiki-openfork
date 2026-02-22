namespace OpenDeepWiki.Models.Admin;

/// <summary>
/// Dashboard statistics response
/// </summary>
public class DashboardStatisticsResponse
{
    /// <summary>
    /// Daily repository statistics
    /// </summary>
    public List<DailyRepositoryStatistic> RepositoryStats { get; set; } = new();

    /// <summary>
    /// Daily user statistics
    /// </summary>
    public List<DailyUserStatistic> UserStats { get; set; } = new();
}

/// <summary>
/// Daily repository statistics
/// </summary>
public class DailyRepositoryStatistic
{
    public DateTime Date { get; set; }
    public int ProcessedCount { get; set; }
    public int SubmittedCount { get; set; }
}

/// <summary>
/// Daily user statistics
/// </summary>
public class DailyUserStatistic
{
    public DateTime Date { get; set; }
    public int NewUserCount { get; set; }
}

/// <summary>
/// Token usage statistics response
/// </summary>
public class TokenUsageStatisticsResponse
{
    public List<DailyTokenUsage> DailyUsages { get; set; } = new();
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalTokens { get; set; }
}

/// <summary>
/// Daily token usage
/// </summary>
public class DailyTokenUsage
{
    public DateTime Date { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
}
