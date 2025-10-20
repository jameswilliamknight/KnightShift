using CommandLine;

namespace KnightShift.Commands;

/// <summary>
/// Base options for all commands
/// </summary>
public class BaseOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Enable verbose output.")]
    public bool Verbose { get; set; }
}

/// <summary>
/// Options for interactive mode
/// </summary>
[Verb("interactive", isDefault: true, HelpText = "Start interactive mode for managing drives and files.")]
public class InteractiveOptions : BaseOptions
{
    [Option('i', "interactive", Required = false, HelpText = "Start interactive mode (shorthand).")]
    public bool Interactive { get; set; }
}

/// <summary>
/// Options for mount command
/// </summary>
[Verb("mount", HelpText = "Mount a drive directly without interactive mode.")]
public class MountOptions : BaseOptions
{
    [Option('d', "device", Required = true, HelpText = "Device path to mount (e.g., /dev/sdb1).")]
    public required string Device { get; set; }

    [Option('p', "path", Required = false, HelpText = "Mount point path. If not specified, uses /mnt/{devicename}.")]
    public string? MountPath { get; set; }

    [Option('t', "type", Required = false, HelpText = "Filesystem type (e.g., ext4, ntfs, vfat).")]
    public string? FileSystemType { get; set; }
}

/// <summary>
/// Options for rename command
/// </summary>
[Verb("rename", HelpText = "Rename folders using regex pattern matching and replacement.")]
public class RenameOptions : BaseOptions
{
    [Option('p', "path", Required = true, HelpText = "Parent folder path containing folders to rename.")]
    public required string Path { get; set; }

    [Option('s', "search", Required = true, HelpText = "Search pattern (regex) to match in folder names.")]
    public required string SearchPattern { get; set; }

    [Option('r', "replace", Required = false, HelpText = "Replacement text. Defaults to empty string.")]
    public string? Replacement { get; set; }

    [Option('y', "yes", Required = false, HelpText = "Skip confirmation prompt.")]
    public bool SkipConfirmation { get; set; }
}

/// <summary>
/// Options for stats command
/// </summary>
[Verb("stats", HelpText = "Display statistics for a folder.")]
public class StatsOptions : BaseOptions
{
    [Option('p', "path", Required = true, HelpText = "Folder path to analyze.")]
    public required string Path { get; set; }
}
