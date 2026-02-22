using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Configuration validation startup filter
/// Validates the integrity of all configurations on application startup
/// </summary>
public class ConfigValidationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<ChatConfigOptions>>().Value;
            
            if (options.ValidateOnStartup)
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<ConfigValidationStartupFilter>>();
                
                // Use IServiceScopeFactory to create a scope for resolving Scoped services
                var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IChatConfigService>();
                
                logger.LogInformation("Validating chat configurations on startup...");
                
                var validationResults = configService.ValidateAllConfigsAsync().Result;
                var hasErrors = false;
                
                foreach (var result in validationResults)
                {
                    if (!result.IsValid)
                    {
                        hasErrors = true;
                        logger.LogWarning(
                            "Configuration validation failed for platform '{Platform}': {Errors}",
                            result.Platform,
                            string.Join(", ", result.Errors));
                        
                        if (result.MissingFields.Count > 0)
                        {
                            logger.LogWarning(
                                "Missing fields for platform '{Platform}': {Fields}",
                                result.Platform,
                                string.Join(", ", result.MissingFields));
                        }
                    }
                    else
                    {
                        logger.LogDebug("Configuration valid for platform: {Platform}", result.Platform);
                    }
                }
                
                if (hasErrors)
                {
                    logger.LogWarning("Some chat configurations have validation errors. Check the logs above for details.");
                }
                else
                {
                    logger.LogInformation("All chat configurations validated successfully.");
                }
            }
            
            next(app);
        };
    }
}

/// <summary>
/// Configuration validator
/// Provides static methods for configuration validation
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// Validate a single configuration
    /// </summary>
    public static ConfigValidationResult Validate(ProviderConfigDto config)
    {
        var errors = new List<string>();
        var missingFields = new List<string>();
        
        // Validate required basic fields
        if (string.IsNullOrWhiteSpace(config.Platform))
        {
            errors.Add("Platform is required");
            missingFields.Add("Platform");
        }
        
        if (string.IsNullOrWhiteSpace(config.DisplayName))
        {
            errors.Add("DisplayName is required");
            missingFields.Add("DisplayName");
        }
        
        // Validate value ranges
        if (config.MessageInterval < 0)
        {
            errors.Add("MessageInterval must be non-negative");
        }
        
        if (config.MaxRetryCount < 0)
        {
            errors.Add("MaxRetryCount must be non-negative");
        }
        
        if (config.MaxRetryCount > 10)
        {
            errors.Add("MaxRetryCount should not exceed 10");
        }
        
        // Validate WebhookUrl format (if provided)
        if (!string.IsNullOrWhiteSpace(config.WebhookUrl))
        {
            if (!Uri.TryCreate(config.WebhookUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add("WebhookUrl must be a valid HTTP or HTTPS URL");
            }
        }
        
        // Validate that ConfigData is valid JSON
        if (!string.IsNullOrWhiteSpace(config.ConfigData))
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(config.ConfigData);
            }
            catch (System.Text.Json.JsonException)
            {
                errors.Add("ConfigData must be valid JSON");
            }
        }
        
        // Validate platform-specific required fields
        if (!string.IsNullOrWhiteSpace(config.Platform) && !string.IsNullOrWhiteSpace(config.ConfigData))
        {
            var platformErrors = ValidatePlatformSpecificFields(config.Platform, config.ConfigData);
            errors.AddRange(platformErrors.Errors);
            missingFields.AddRange(platformErrors.MissingFields);
        }
        
        return new ConfigValidationResult
        {
            Platform = config.Platform,
            IsValid = errors.Count == 0,
            Errors = errors,
            MissingFields = missingFields
        };
    }
    
    /// <summary>
    /// Validate platform-specific required fields
    /// </summary>
    private static (List<string> Errors, List<string> MissingFields) ValidatePlatformSpecificFields(
        string platform, 
        string configData)
    {
        var errors = new List<string>();
        var missingFields = new List<string>();
        
        var requiredFields = GetRequiredFieldsForPlatform(platform);
        
        try
        {
            var configJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(configData);
            if (configJson == null)
            {
                errors.Add("ConfigData is empty or invalid");
                return (errors, missingFields);
            }
            
            foreach (var field in requiredFields)
            {
                if (!configJson.TryGetValue(field, out var value) || 
                    value == null || 
                    string.IsNullOrWhiteSpace(value.ToString()))
                {
                    errors.Add($"Required field '{field}' is missing or empty in ConfigData for platform '{platform}'");
                    missingFields.Add(field);
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            errors.Add("ConfigData is not valid JSON");
        }
        
        return (errors, missingFields);
    }
    
    /// <summary>
    /// Get required fields for a platform
    /// </summary>
    private static string[] GetRequiredFieldsForPlatform(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "feishu" => new[] { "AppId", "AppSecret" },
            "qq" => new[] { "AppId", "Token" },
            "wechat" => new[] { "AppId", "AppSecret", "Token", "EncodingAesKey" },
            _ => Array.Empty<string>() // No specific fields required by default
        };
    }
}
