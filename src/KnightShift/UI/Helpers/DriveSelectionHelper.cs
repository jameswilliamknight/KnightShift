using Spectre.Console;
using KnightShift.Models;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Helper class for rendering drive selection UI components
/// </summary>
public static class DriveSelectionHelper
{
    /// <summary>
    /// Shows help panel explaining mount points
    /// </summary>
    public static void ShowMountPointHelp(string defaultPath)
    {
        AnsiConsole.WriteLine();
        var helpPanel = new Panel(
            new Markup(
                $"[bold underline]What is a mount point?[/]\n\n" +
                $"A mount point is a directory where the drive's contents will be accessible.\n" +
                $"After mounting, you can access files like: {Markup.Escape(defaultPath)}/photos/\n\n" +

                $"[bold underline]Recommendations:[/]\n" +
                $"  • Use the default: [cyan]{Markup.Escape(defaultPath)}[/]\n" +
                $"  • Or choose another location: /mnt/mydrive, /media/usb, etc.\n" +
                $"  • Must be an absolute path (starts with /)\n" +
                $"  • Directory will be created if it doesn't exist\n\n" +

                $"[bold underline]Safety:[/]\n" +
                $"  {StyleGuide.WarningIcon} Avoid system directories: /, /home, /etc, /usr, /var\n" +
                $"  {StyleGuide.CheckMark} Safe locations: /mnt/*, /media/*\n\n" +

                $"[{StyleGuide.Muted}]Press Enter to continue...[/]"
            )
        )
        {
            Header = new PanelHeader("Mount Point Help"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Aqua),
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(helpPanel);
        Console.ReadKey(true);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a summary of found drives with counts
    /// </summary>
    public static void RenderDriveCountSummary(int totalCount, int mountedCount, int unmountedCount)
    {
        if (mountedCount > 0 && unmountedCount > 0)
        {
            AnsiConsole.MarkupLine($"Found [bold]{totalCount}[/] drive(s): [green]{mountedCount} mounted[/], [yellow]{unmountedCount} unmounted[/]");
        }
        else if (mountedCount > 0)
        {
            AnsiConsole.MarkupLine($"Found [bold]{totalCount}[/] drive(s) (all already mounted):");
        }
        else
        {
            AnsiConsole.MarkupLine($"Found [bold]{totalCount}[/] unmounted drive(s):");
        }
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders the mount confirmation panel
    /// </summary>
    public static void RenderMountConfirmationPanel(DetectedDrive selectedDrive, string mountPoint)
    {
        var confirmPanel = new Panel(
            new Markup(
                $"[bold]Drive:[/] {Markup.Escape(selectedDrive.DisplayString)}\n" +
                $"[bold]Mount point:[/] {Markup.Escape(mountPoint)}\n" +
                $"[bold]Filesystem:[/] {selectedDrive.FileSystemType ?? "auto-detect"}\n\n" +
                $"[{StyleGuide.Muted}]The drive will be mounted using sudo privileges.[/]"
            )
        )
        {
            Header = new PanelHeader("Confirm Mount Operation"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(confirmPanel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Generates default mount point path from drive info
    /// </summary>
    public static string GetDefaultMountPoint(DetectedDrive drive)
    {
        var sanitizedName = drive.IsWindowsDrive
            ? drive.DeviceName.TrimEnd(':').ToLower()
            : drive.DeviceName;
        return $"/mnt/{sanitizedName}";
    }
}
