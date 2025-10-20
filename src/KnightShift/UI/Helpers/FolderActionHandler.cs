using Spectre.Console;
using KnightShift.Models;
using System.Diagnostics;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles folder actions like opening in terminal or VS Code
/// </summary>
public static class FolderActionHandler
{
    public const string ActionRemoveText = "Regex Replace Folder Names";
    public const string ActionProperties = "View Properties";
    public const string ActionOpenTerminal = "Open in Terminal";
    public const string ActionOpenVSCode = "Open in VS Code";
    public const string ActionGoBack = "← Go Back";

    /// <summary>
    /// Shows action menu for a folder and returns the selected action
    /// </summary>
    public static string ShowActionMenu(FileSystemEntry entry)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(StyleGuide.CreateTitleRule($"Actions: {entry.Name}"));
        AnsiConsole.WriteLine();

        var choices = new List<string>
        {
            "Navigate into folder",
            ActionRemoveText,
            ActionProperties,
            ActionOpenTerminal,
            ActionOpenVSCode,
            ActionGoBack
        };

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]What would you like to do?[/]")
                .AddChoices(choices)
        );

        return selected;
    }

    /// <summary>
    /// Opens a folder in the default terminal emulator
    /// </summary>
    public static void OpenInTerminal(string path)
    {
        try
        {
            // Try to open in the default terminal
            // This works for most Linux terminals
            var startInfo = new ProcessStartInfo
            {
                FileName = "x-terminal-emulator",
                Arguments = $"-e 'cd \"{path}\" && exec $SHELL'",
                UseShellExecute = true
            };

            Process.Start(startInfo);
            AnsiConsole.Write(StyleGuide.Success("Opened in terminal."));
        }
        catch
        {
            AnsiConsole.Write(StyleGuide.Warning($"Could not open terminal. Path: {path}"));
        }

        Task.Delay(1500).Wait();
    }

    /// <summary>
    /// Opens a folder in VS Code
    /// </summary>
    public static void OpenInVSCode(string path)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
            AnsiConsole.Write(StyleGuide.Success("Opened in VS Code."));
        }
        catch
        {
            AnsiConsole.Write(StyleGuide.Warning("Could not open VS Code. Is it installed?"));
        }

        Task.Delay(1500).Wait();
    }
}
