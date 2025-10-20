using KnightShift.Models;

namespace KnightShift.Services;

/// <summary>
/// Query interface for reading user settings
/// </summary>
public interface ISettingsQueries
{
    /// <summary>
    /// Loads user settings from storage
    /// </summary>
    /// <returns>User settings, or default settings if none exist</returns>
    Task<UserSettings> LoadAsync();
}
