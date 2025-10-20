using KnightShift.Models;

namespace KnightShift.Services;

/// <summary>
/// Command interface for writing user settings
/// </summary>
public interface ISettingsCommands
{
    /// <summary>
    /// Saves user settings to storage
    /// </summary>
    Task SaveAsync(UserSettings settings);
}
