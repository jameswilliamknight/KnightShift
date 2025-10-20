namespace KnightShift.Models;

/// <summary>
/// Result of a file browser navigation action
/// </summary>
public class NavigationResult
{
    /// <summary>
    /// Whether to continue browsing (false = exit browser)
    /// </summary>
    public bool ShouldContinue { get; set; }

    /// <summary>
    /// New path to navigate to (null = stay at current path)
    /// </summary>
    public string? NewPath { get; set; }

    /// <summary>
    /// Name of the selected folder (used for navigation history)
    /// </summary>
    public string? SelectedFolderName { get; set; }

    /// <summary>
    /// Creates a NavigationResult
    /// </summary>
    public NavigationResult(bool shouldContinue, string? newPath = null, string? selectedFolderName = null)
    {
        ShouldContinue = shouldContinue;
        NewPath = newPath;
        SelectedFolderName = selectedFolderName;
    }
}
