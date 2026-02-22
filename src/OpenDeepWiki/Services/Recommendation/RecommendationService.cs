using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Models.Recommendation;

namespace OpenDeepWiki.Services.Recommendation;

/// <summary>
/// Recommendation service
/// Implements a multi-dimensional hybrid recommendation algorithm
/// </summary>
public class RecommendationService
{
    private readonly IContext _context;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(IContext context, ILogger<RecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get recommended repository list
    /// </summary>
    public async Task<RecommendationResponse> GetRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = GetConfigByStrategy(request.Strategy);

        // 1. Get candidate repository pool (public and completed repositories)
        var candidateQuery = _context.Repositories
            .AsNoTracking()
            .Where(r => r.IsPublic && r.Status == RepositoryStatus.Completed && !r.IsDeleted);

        // Language filter
        if (!string.IsNullOrEmpty(request.LanguageFilter))
        {
            candidateQuery = candidateQuery.Where(r => r.PrimaryLanguage == request.LanguageFilter);
        }

        // Exclude repositories the user marked as not interested
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var dislikedRepoIds = await _context.UserDislikes
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId)
                .Select(d => d.RepositoryId)
                .ToListAsync(cancellationToken);

            if (dislikedRepoIds.Count > 0)
            {
                candidateQuery = candidateQuery.Where(r => !dislikedRepoIds.Contains(r.Id));
            }
        }

        var candidates = await candidateQuery.ToListAsync(cancellationToken);
        var totalCandidates = candidates.Count;

        if (candidates.Count == 0)
        {
            return new RecommendationResponse
            {
                Items = new List<RecommendedRepository>(),
                Strategy = request.Strategy,
                TotalCandidates = 0
            };
        }

        // 2. Get user-related data (if user ID is available)
        UserPreferenceData? userPreference = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            userPreference = await GetUserPreferenceDataAsync(request.UserId, cancellationToken);
        }

        // 3. Get collaborative filtering data
        var collaborativeScores = new Dictionary<string, double>();
        if (!string.IsNullOrEmpty(request.UserId) && config.CollaborativeWeight > 0)
        {
            collaborativeScores = await CalculateCollaborativeScoresAsync(
                request.UserId, candidates.Select(c => c.Id).ToList(), cancellationToken);
        }

        // 4. Calculate composite score for each repository
        var scoredRepos = candidates.Select(repo =>
        {
            var breakdown = CalculateScoreBreakdown(repo, userPreference, collaborativeScores, config);
            var finalScore = CalculateFinalScore(breakdown, config);

            return new RecommendedRepository
            {
                Id = repo.Id,
                RepoName = repo.RepoName,
                OrgName = repo.OrgName,
                PrimaryLanguage = repo.PrimaryLanguage,
                StarCount = repo.StarCount,
                ForkCount = repo.ForkCount,
                BookmarkCount = repo.BookmarkCount,
                SubscriptionCount = repo.SubscriptionCount,
                ViewCount = repo.ViewCount,
                CreatedAt = repo.CreatedAt,
                UpdatedAt = repo.UpdatedAt,
                Score = finalScore,
                ScoreBreakdown = breakdown,
                RecommendReason = GenerateRecommendReason(breakdown, userPreference)
            };
        })
        .OrderByDescending(r => r.Score)
        .Take(request.Limit)
        .ToList();

        return new RecommendationResponse
        {
            Items = scoredRepos,
            Strategy = request.Strategy,
            TotalCandidates = totalCandidates
        };
    }


    /// <summary>
    /// Get configuration by strategy
    /// </summary>
    private RecommendationConfig GetConfigByStrategy(string strategy)
    {
        return strategy.ToLower() switch
        {
            // Popular strategy: emphasizes popularity and subscriptions
            "popular" => new RecommendationConfig
            {
                PopularityWeight = 0.40,
                SubscriptionWeight = 0.30,
                TimeDecayWeight = 0.20,
                UserPreferenceWeight = 0.05,
                PrivateRepoLanguageWeight = 0.05,
                CollaborativeWeight = 0.00
            },
            // Personalized strategy: emphasizes user preferences
            "personalized" => new RecommendationConfig
            {
                PopularityWeight = 0.10,
                SubscriptionWeight = 0.10,
                TimeDecayWeight = 0.10,
                UserPreferenceWeight = 0.30,
                PrivateRepoLanguageWeight = 0.25,
                CollaborativeWeight = 0.15
            },
            // Explore strategy: increases randomness and long-tail content
            "explore" => new RecommendationConfig
            {
                PopularityWeight = 0.15,
                SubscriptionWeight = 0.10,
                TimeDecayWeight = 0.25,
                UserPreferenceWeight = 0.15,
                PrivateRepoLanguageWeight = 0.15,
                CollaborativeWeight = 0.20
            },
            // Default hybrid strategy
            _ => new RecommendationConfig()
        };
    }

    /// <summary>
    /// Calculate popularity score
    /// Uses logarithmic normalization to prevent large values from dominating
    /// </summary>
    private double CalculatePopularityScore(Repository repo)
    {
        const double starWeight = 0.5;
        const double forkWeight = 0.3;
        const double viewWeight = 0.2;
        const double maxScore = 6.0; // log10(1000000) ≈ 6

        var normalizedStars = Math.Log10(repo.StarCount + 1);
        var normalizedForks = Math.Log10(repo.ForkCount + 1);
        var normalizedViews = Math.Log10(repo.ViewCount + 1);

        var score = (normalizedStars * starWeight +
                     normalizedForks * forkWeight +
                     normalizedViews * viewWeight) / maxScore;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculate subscription score
    /// Subscription count reflects users' sustained interest
    /// </summary>
    private double CalculateSubscriptionScore(Repository repo)
    {
        const double maxScore = 4.0; // log10(10000) = 4
        var normalizedSubs = Math.Log10(repo.SubscriptionCount + 1);
        return Math.Min(normalizedSubs / maxScore, 1.0);
    }

    /// <summary>
    /// Calculate time decay score
    /// Recently updated repositories score higher
    /// </summary>
    private double CalculateTimeDecayScore(Repository repo)
    {
        var lastUpdate = repo.UpdatedAt ?? repo.CreatedAt;
        var daysSinceUpdate = (DateTime.UtcNow - lastUpdate).TotalDays;

        // Half-life set to 60 days
        const double halfLife = 60.0;
        return Math.Exp(-0.693 * daysSinceUpdate / halfLife);
    }

    /// <summary>
    /// Calculate user preference score
    /// Based on language preferences from user's historical behavior
    /// </summary>
    private double CalculateUserPreferenceScore(Repository repo, UserPreferenceData? userPref)
    {
        if (userPref == null) return 0.5; // Return neutral score when no user data is available

        double score = 0;

        // Language match (60%)
        if (!string.IsNullOrEmpty(repo.PrimaryLanguage) &&
            userPref.LanguageWeights.TryGetValue(repo.PrimaryLanguage, out var langWeight))
        {
            score += 0.6 * langWeight;
        }

        // Exclude viewed/bookmarked repositories, give bonus points to new content (40%)
        if (!userPref.ViewedRepoIds.Contains(repo.Id) &&
            !userPref.BookmarkedRepoIds.Contains(repo.Id))
        {
            score += 0.4;
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculate private repository language match score
    /// Based on the language distribution of user's own repositories
    /// </summary>
    private double CalculatePrivateRepoLanguageScore(Repository repo, UserPreferenceData? userPref)
    {
        if (userPref == null || string.IsNullOrEmpty(repo.PrimaryLanguage))
            return 0.3; // Return lower score when no data is available

        if (userPref.PrivateRepoLanguages.TryGetValue(repo.PrimaryLanguage, out var weight))
        {
            return weight;
        }

        return 0.1; // Give lower score when language doesn't match
    }


    /// <summary>
    /// Calculate score breakdown
    /// </summary>
    private ScoreBreakdown CalculateScoreBreakdown(
        Repository repo,
        UserPreferenceData? userPref,
        Dictionary<string, double> collaborativeScores,
        RecommendationConfig config)
    {
        return new ScoreBreakdown
        {
            Popularity = CalculatePopularityScore(repo),
            Subscription = CalculateSubscriptionScore(repo),
            TimeDecay = CalculateTimeDecayScore(repo),
            UserPreference = CalculateUserPreferenceScore(repo, userPref),
            PrivateRepoLanguage = CalculatePrivateRepoLanguageScore(repo, userPref),
            Collaborative = collaborativeScores.GetValueOrDefault(repo.Id, 0.3)
        };
    }

    /// <summary>
    /// Calculate final score
    /// </summary>
    private double CalculateFinalScore(ScoreBreakdown breakdown, RecommendationConfig config)
    {
        return breakdown.Popularity * config.PopularityWeight +
               breakdown.Subscription * config.SubscriptionWeight +
               breakdown.TimeDecay * config.TimeDecayWeight +
               breakdown.UserPreference * config.UserPreferenceWeight +
               breakdown.PrivateRepoLanguage * config.PrivateRepoLanguageWeight +
               breakdown.Collaborative * config.CollaborativeWeight;
    }

    /// <summary>
    /// Generate recommendation reason
    /// </summary>
    private string GenerateRecommendReason(ScoreBreakdown breakdown, UserPreferenceData? userPref)
    {
        var reasons = new List<string>();

        if (breakdown.Popularity > 0.7)
            reasons.Add("Popular project");
        if (breakdown.Subscription > 0.6)
            reasons.Add("Highly subscribed");
        if (breakdown.TimeDecay > 0.8)
            reasons.Add("Recently active");
        if (breakdown.UserPreference > 0.6 && userPref != null)
            reasons.Add("Matches your preferences");
        if (breakdown.PrivateRepoLanguage > 0.6 && userPref != null)
            reasons.Add("Matches your tech stack");
        if (breakdown.Collaborative > 0.5)
            reasons.Add("Recommended by similar users");

        return reasons.Count > 0 ? string.Join(", ", reasons) : "General recommendation";
    }

    /// <summary>
    /// Get user preference data
    /// </summary>
    private async Task<UserPreferenceData> GetUserPreferenceDataAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = new UserPreferenceData();

        // 1. Get repository IDs viewed by the user
        var viewedRepoIds = await _context.UserActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.RepositoryId != null)
            .Select(a => a.RepositoryId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        result.ViewedRepoIds = viewedRepoIds.ToHashSet();

        // 2. Get repository IDs bookmarked by the user
        var bookmarkedRepoIds = await _context.UserBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => b.RepositoryId)
            .ToListAsync(cancellationToken);
        result.BookmarkedRepoIds = bookmarkedRepoIds.ToHashSet();

        // 3. Calculate language preferences (based on user activity)
        var languageActivities = await _context.UserActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.Language != null)
            .GroupBy(a => a.Language!)
            .Select(g => new { Language = g.Key, TotalWeight = g.Sum(a => a.Weight) })
            .ToListAsync(cancellationToken);

        if (languageActivities.Count > 0)
        {
            var maxWeight = languageActivities.Max(l => l.TotalWeight);
            foreach (var lang in languageActivities)
            {
                result.LanguageWeights[lang.Language] = (double)lang.TotalWeight / maxWeight;
            }
        }

        // 4. Get language distribution of user's private repositories
        var userRepoLanguages = await _context.Repositories
            .AsNoTracking()
            .Where(r => r.OwnerUserId == userId && r.PrimaryLanguage != null)
            .GroupBy(r => r.PrimaryLanguage!)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        if (userRepoLanguages.Count > 0)
        {
            var totalCount = userRepoLanguages.Sum(l => l.Count);
            foreach (var lang in userRepoLanguages)
            {
                result.PrivateRepoLanguages[lang.Language] = (double)lang.Count / totalCount;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate collaborative filtering scores
    /// Recommendations based on similar users' behavior
    /// </summary>
    private async Task<Dictionary<string, double>> CalculateCollaborativeScoresAsync(
        string userId,
        List<string> candidateRepoIds,
        CancellationToken cancellationToken)
    {
        var scores = new Dictionary<string, double>();

        // 1. Get the target user's bookmarks and subscriptions
        var userRepoIds = await _context.UserBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => b.RepositoryId)
            .Union(_context.UserSubscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.RepositoryId))
            .ToListAsync(cancellationToken);

        if (userRepoIds.Count == 0)
        {
            return scores;
        }

        // 2. Find other users who bookmarked/subscribed to the same repositories
        var similarUserIds = await _context.UserBookmarks
            .AsNoTracking()
            .Where(b => userRepoIds.Contains(b.RepositoryId) && b.UserId != userId)
            .Select(b => b.UserId)
            .Union(_context.UserSubscriptions
                .AsNoTracking()
                .Where(s => userRepoIds.Contains(s.RepositoryId) && s.UserId != userId)
                .Select(s => s.UserId))
            .Distinct()
            .Take(100) // Limit the number of similar users
            .ToListAsync(cancellationToken);

        if (similarUserIds.Count == 0)
        {
            return scores;
        }

        // 3. Get repositories bookmarked/subscribed by similar users and their frequency
        var similarUserRepos = await _context.UserBookmarks
            .AsNoTracking()
            .Where(b => similarUserIds.Contains(b.UserId) && candidateRepoIds.Contains(b.RepositoryId))
            .GroupBy(b => b.RepositoryId)
            .Select(g => new { RepoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var subscriptionRepos = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => similarUserIds.Contains(s.UserId) && candidateRepoIds.Contains(s.RepositoryId))
            .GroupBy(s => s.RepositoryId)
            .Select(g => new { RepoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // 4. Merge and calculate scores
        var combinedScores = new Dictionary<string, int>();
        foreach (var item in similarUserRepos)
        {
            combinedScores[item.RepoId] = item.Count;
        }
        foreach (var item in subscriptionRepos)
        {
            if (combinedScores.ContainsKey(item.RepoId))
                combinedScores[item.RepoId] += item.Count * 2; // Subscriptions have higher weight
            else
                combinedScores[item.RepoId] = item.Count * 2;
        }

        if (combinedScores.Count > 0)
        {
            var maxCount = combinedScores.Values.Max();
            foreach (var kvp in combinedScores)
            {
                scores[kvp.Key] = (double)kvp.Value / maxCount;
            }
        }

        return scores;
    }

    /// <summary>
    /// Record user activity
    /// </summary>
    public async Task<bool> RecordActivityAsync(
        RecordActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<UserActivityType>(request.ActivityType, true, out var activityType))
            {
                _logger.LogWarning("Invalid activity type: {ActivityType}", request.ActivityType);
                return false;
            }

            var weight = activityType switch
            {
                UserActivityType.View => 1,
                UserActivityType.Search => 2,
                UserActivityType.Bookmark => 3,
                UserActivityType.Subscribe => 4,
                UserActivityType.Analyze => 5,
                _ => 1
            };

            var activity = new UserActivity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                RepositoryId = request.RepositoryId,
                ActivityType = activityType,
                Weight = weight,
                Duration = request.Duration,
                SearchQuery = request.SearchQuery,
                Language = request.Language
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record user activity");
            return false;
        }
    }

    /// <summary>
    /// Update user preference cache
    /// </summary>
    public async Task UpdateUserPreferenceCacheAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prefData = await GetUserPreferenceDataAsync(userId, cancellationToken);

            var cache = await _context.UserPreferenceCaches
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (cache == null)
            {
                cache = new UserPreferenceCache
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId
                };
                _context.UserPreferenceCaches.Add(cache);
            }

            cache.LanguageWeights = JsonSerializer.Serialize(prefData.LanguageWeights);
            cache.PrivateRepoLanguages = JsonSerializer.Serialize(prefData.PrivateRepoLanguages);
            cache.LastCalculatedAt = DateTime.UtcNow;
            cache.UpdateTimestamp();

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user preference cache for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Mark repository as not interested
    /// </summary>
    public async Task<bool> MarkAsDislikedAsync(
        DislikeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if already marked
            var existing = await _context.UserDislikes
                .FirstOrDefaultAsync(d => d.UserId == request.UserId && d.RepositoryId == request.RepositoryId, cancellationToken);

            if (existing != null)
            {
                return true; // Already marked
            }

            var dislike = new UserDislike
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                RepositoryId = request.RepositoryId,
                Reason = request.Reason
            };

            _context.UserDislikes.Add(dislike);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark repository as disliked");
            return false;
        }
    }

    /// <summary>
    /// Remove not-interested mark
    /// </summary>
    public async Task<bool> RemoveDislikeAsync(
        string userId,
        string repositoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dislike = await _context.UserDislikes
                .FirstOrDefaultAsync(d => d.UserId == userId && d.RepositoryId == repositoryId, cancellationToken);

            if (dislike == null)
            {
                return true;
            }

            _context.UserDislikes.Remove(dislike);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove dislike");
            return false;
        }
    }

    /// <summary>
    /// Get available programming languages list
    /// </summary>
    public async Task<AvailableLanguagesResponse> GetAvailableLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        var languages = await _context.Repositories
            .AsNoTracking()
            .Where(r => r.IsPublic && r.Status == RepositoryStatus.Completed && !r.IsDeleted && r.PrimaryLanguage != null)
            .GroupBy(r => r.PrimaryLanguage!)
            .Select(g => new LanguageInfo
            {
                Name = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(l => l.Count)
            .ToListAsync(cancellationToken);

        return new AvailableLanguagesResponse { Languages = languages };
    }
}

/// <summary>
/// User preference data (used in memory)
/// </summary>
public class UserPreferenceData
{
    public HashSet<string> ViewedRepoIds { get; set; } = new();
    public HashSet<string> BookmarkedRepoIds { get; set; } = new();
    public Dictionary<string, double> LanguageWeights { get; set; } = new();
    public Dictionary<string, double> PrivateRepoLanguages { get; set; } = new();
}
