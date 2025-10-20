namespace KnightShift.Models;

/// <summary>
/// User preferences and settings for KnightShift
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Skip confirmation prompt when only one drive is available
    /// </summary>
    public bool SkipSingleDriveConfirmation { get; set; } = false;
}
