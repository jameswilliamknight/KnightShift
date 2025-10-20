using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;

namespace KnightShift.UI;

/// <summary>
/// UI component for displaying folder properties and statistics
/// </summary>
public class PropertiesPanel
{
    private readonly FileSystemBrowserService _browserService;

    public PropertiesPanel(FileSystemBrowserService browserService)
    {
        _browserService = browserService;
    }

    /// <summary>
    /// Shows properties for a selected folder
    /// </summary>
    public async Task ShowAsync(FileSystemEntry entry)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(StyleGuide.CreateTitleRule($"Properties: {entry.Name}"));
        AnsiConsole.WriteLine();

        // Calculate properties with a progress indicator
        var properties = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Calculating folder statistics...", async ctx =>
            {
                var size = await Task.Run(() => _browserService.CalculateDirectorySize(entry.FullPath));
                var (fileCount, folderCount) = await Task.Run(() => _browserService.CountContents(entry.FullPath, recursive: true));

                return new
                {
                    Size = size,
                    FileCount = fileCount,
                    FolderCount = folderCount
                };
            });

        // Create properties table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.DodgerBlue1);

        table.AddColumn(new TableColumn("Property").Centered());
        table.AddColumn(new TableColumn("Value").Centered());

        table.AddRow("Name", $"[bold]{entry.Name}[/]");
        table.AddRow("Full Path", entry.FullPath);
        table.AddRow("Type", entry.IsDirectory ? "Directory" : "File");
        table.AddRow("Size", FormatBytes(properties.Size));
        table.AddRow("Files", $"{properties.FileCount:N0}");
        table.AddRow("Folders", $"{properties.FolderCount:N0}");
        table.AddRow("Last Modified", entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));

        var panel = StyleGuide.CreateFloatingPanel("Folder Properties", table);
        AnsiConsole.Write(panel);

        AnsiConsole.WriteLine();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
