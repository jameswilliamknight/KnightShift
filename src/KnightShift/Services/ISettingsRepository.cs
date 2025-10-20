namespace KnightShift.Services;

/// <summary>
/// Repository interface for persisting user settings (combines queries and commands)
/// For new code, prefer using ISettingsQueries and ISettingsCommands directly
/// </summary>
public interface ISettingsRepository : ISettingsQueries, ISettingsCommands
{
}
