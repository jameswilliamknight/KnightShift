using System.Text.Json;
using KnightShift.Models;

namespace KnightShift.Services;

/// <summary>
/// JSON file-based implementation of settings repository
/// Follows XDG Base Directory Specification for Linux
/// Implements both ISettingsQueries (read) and ISettingsCommands (write)
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly string _settingsFilePath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsRepository()
    {
        // Follow XDG Base Directory Specification
        // Use XDG_CONFIG_HOME if set, otherwise default to ~/.config
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configHome = Path.Combine(homeDir, ".config");
        }

        var appConfigDir = Path.Combine(configHome, "knightshift");
        _settingsFilePath = Path.Combine(appConfigDir, "settings.json");
    }

    /// <summary>
    /// Loads user settings from JSON file
    /// </summary>
    public async Task<UserSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Return default settings if file doesn't exist
                return new UserSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, _jsonOptions);

            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            // Log error and return default settings
            Console.Error.WriteLine($"Warning: Failed to load settings: {ex.Message}");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Saves user settings to JSON file
    /// </summary>
    public async Task SaveAsync(UserSettings settings)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to save settings: {ex.Message}");
            throw;
        }
    }
}
