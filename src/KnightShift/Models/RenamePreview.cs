namespace KnightShift.Models;

/// <summary>
/// Represents a match position for regex highlighting
/// </summary>
public class MatchPosition
{
    public int Start { get; init; }
    public int Length { get; init; }
}

/// <summary>
/// Represents a before/after preview of a folder rename operation
/// </summary>
public class RenamePreview
{
    /// <summary>
    /// Original folder name
    /// </summary>
    public required string OriginalName { get; init; }

    /// <summary>
    /// New folder name after text removal
    /// </summary>
    public required string NewName { get; init; }

    /// <summary>
    /// Full original path
    /// </summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Full new path
    /// </summary>
    public required string NewPath { get; init; }

    /// <summary>
    /// Whether this rename would cause a conflict (target already exists)
    /// </summary>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Match positions for highlighting in the original name
    /// </summary>
    public List<MatchPosition> MatchPositions { get; init; } = new();

    /// <summary>
    /// Whether the name would actually change
    /// </summary>
    public bool WillChange => OriginalName != NewName;

    /// <summary>
    /// Whether the result would be empty or whitespace-only
    /// </summary>
    public bool HasEmptyResult => string.IsNullOrWhiteSpace(NewName);

    /// <summary>
    /// Status indicator for display
    /// </summary>
    public string StatusIcon => HasConflict ? "⚠️" : (WillChange ? "✓" : "−");
}
