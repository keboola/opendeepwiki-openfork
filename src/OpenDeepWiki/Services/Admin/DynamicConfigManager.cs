using Microsoft.Extensions.Options;
using OpenDeepWiki.Services.Wiki;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Dynamic configuration manager for updating configuration from system settings
/// </summary>
public interface IDynamicConfigManager
{
    /// <summary>
    /// Refresh WikiGeneratorOptions configuration
    /// </summary>
    Task RefreshWikiGeneratorOptionsAsync();
}

/// <summary>
/// Dynamic configuration manager implementation
/// </summary>
public class DynamicConfigManager : IDynamicConfigManager
{
    private readonly IOptionsMonitor<WikiGeneratorOptions> _optionsMonitor;
    private readonly IAdminSettingsService _settingsService;
    private readonly ILogger<DynamicConfigManager> _logger;

    public DynamicConfigManager(
        IOptionsMonitor<WikiGeneratorOptions> optionsMonitor,
        IAdminSettingsService settingsService,
        ILogger<DynamicConfigManager> logger)
    {
        _optionsMonitor = optionsMonitor;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Refresh WikiGeneratorOptions configuration
    /// </summary>
    public async Task RefreshWikiGeneratorOptionsAsync()
    {
        try
        {
            // Get current configuration
            var currentOptions = _optionsMonitor.CurrentValue;

            // Get all AI-related system settings
            var aiSettings = await _settingsService.GetSettingsAsync("ai");

            // Apply settings to configuration object
            foreach (var setting in aiSettings)
            {
                SystemSettingDefaults.ApplySettingToOption(currentOptions, setting.Key, setting.Value ?? string.Empty);
            }

            _logger.LogDebug("WikiGeneratorOptions configuration refreshed from system settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh WikiGeneratorOptions configuration");
            throw;
        }
    }
}
