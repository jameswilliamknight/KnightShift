using KnightShift.Models;
using System.Text.RegularExpressions;

namespace KnightShift.Services;

/// <summary>
/// Service for renaming folders using regex pattern matching and replacement
/// </summary>
public class FolderRenameService
{
    private readonly FileSystemBrowserService _browserService;

    public FolderRenameService(FileSystemBrowserService browserService)
    {
        _browserService = browserService;
    }

    /// <summary>
    /// Generates a preview of rename operations using regex for immediate child folders
    /// </summary>
    public List<RenamePreview> GenerateRenamePreview(string parentPath, string searchPattern, string replacement, bool useRegex = true)
    {
        var previews = new List<RenamePreview>();
        var directories = _browserService.GetImmediateChildDirectories(parentPath);

        foreach (var directory in directories)
        {
            var originalName = directory.Name;
            string newName;
            var matchPositions = new List<MatchPosition>();

            // If search pattern is empty, just show all folders as-is
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                newName = originalName;
            }
            else
            {
                try
                {
                    if (useRegex)
                    {
                        // Capture match positions for highlighting
                        var regex = new Regex(searchPattern);
                        var matches = regex.Matches(originalName);
                        foreach (Match match in matches)
                        {
                            matchPositions.Add(new MatchPosition
                            {
                                Start = match.Index,
                                Length = match.Length
                            });
                        }

                        // Use regex replace with .NET flavor
                        newName = Regex.Replace(originalName, searchPattern, replacement ?? "");
                    }
                    else
                    {
                        // Simple string replace - find all occurrences for highlighting
                        int index = 0;
                        while ((index = originalName.IndexOf(searchPattern, index, StringComparison.Ordinal)) != -1)
                        {
                            matchPositions.Add(new MatchPosition
                            {
                                Start = index,
                                Length = searchPattern.Length
                            });
                            index += searchPattern.Length;
                        }

                        newName = originalName.Replace(searchPattern, replacement ?? "");
                    }

                    // Trim any extra spaces that might result from the removal
                    newName = string.Join(" ", newName.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                    // Sanitize filename (remove invalid characters)
                    var invalidChars = Path.GetInvalidFileNameChars();
                    newName = string.Join("_", newName.Split(invalidChars));

                    // Ensure we have a valid name
                    if (string.IsNullOrWhiteSpace(newName))
                        newName = originalName;
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern - keep original name
                    newName = originalName;
                    matchPositions.Clear();
                }
            }

            var newPath = Path.Combine(Path.GetDirectoryName(directory.FullPath)!, newName);

            // Check if the new name would conflict with an existing directory
            bool hasConflict = newName != originalName && Directory.Exists(newPath);

            previews.Add(new RenamePreview
            {
                OriginalName = originalName,
                NewName = newName,
                OriginalPath = directory.FullPath,
                NewPath = newPath,
                HasConflict = hasConflict,
                MatchPositions = matchPositions
            });
        }

        return previews;
    }

    /// <summary>
    /// Validates if a regex pattern is valid
    /// </summary>
    public bool IsValidRegexPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            Regex.Match("", pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a pattern and returns any error message
    /// </summary>
    public (bool IsValid, string ErrorMessage) ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return (false, "Pattern cannot be empty");

        try
        {
            Regex.Match("", pattern);
            return (true, string.Empty);
        }
        catch (ArgumentException ex)
        {
            return (false, $"Invalid regex: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if any preview would result in an empty name
    /// </summary>
    public bool HasEmptyResults(List<RenamePreview> previews)
    {
        return previews.Any(p => p.HasEmptyResult);
    }

    /// <summary>
    /// Applies the rename operations from a list of previews
    /// </summary>
    public async Task<RenameResult> ApplyRenamesAsync(List<RenamePreview> previews)
    {
        var result = new RenameResult();

        foreach (var preview in previews)
        {
            // Skip if no change needed
            if (!preview.WillChange)
            {
                result.Skipped++;
                continue;
            }

            // Skip if there's a conflict
            if (preview.HasConflict)
            {
                result.Failed++;
                result.Errors.Add($"Conflict: {preview.OriginalName} -> {preview.NewName} (target already exists)");
                continue;
            }

            try
            {
                // Perform the rename
                await Task.Run(() => Directory.Move(preview.OriginalPath, preview.NewPath));
                result.Successful++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Failed to rename {preview.OriginalName}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Result of a batch rename operation
    /// </summary>
    public class RenameResult
    {
        public int Successful { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();

        public int Total => Successful + Failed + Skipped;
        public bool HasErrors => Failed > 0;
    }
}
