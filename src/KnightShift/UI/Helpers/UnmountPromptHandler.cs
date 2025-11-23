using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles unmount prompts and operations for session-mounted drives
/// </summary>
public static class UnmountPromptHandler
{
    /// <summary>
    /// Shows unmount prompt and handles user selection
    /// Returns true if user wants to unmount drives
    /// </summary>
    public static async Task<List<string>?> ShowUnmountPromptAsync(
        List<string> mountedDrives,
        ISettingsRepository settingsRepository)
    {
        var settings = await settingsRepository.LoadAsync();

        if (!settings.PromptToUnmountOnExit)
        {
            return null; // User disabled this prompt
        }

        AnsiConsole.Clear();

        // Show informational panel
        var infoPanel = new Panel(
            new Markup(
                $"[{StyleGuide.Primary}]Drives Mounted During This Session[/]\n\n" +
                $"The following drives were mounted while using KnightShift:\n\n" +
                string.Join("\n", mountedDrives.Select(m => $"  {StyleGuide.Bullet} [cyan]{Markup.Escape(m)}[/]")) +
                $"\n\n[{StyleGuide.Muted}]Would you like to unmount them before exiting?[/]"
            )
        )
        {
            Header = new PanelHeader($"{StyleGuide.Drive} Session Cleanup", Justify.Center),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(StyleGuide.PrimaryColor),
            Padding = new Padding(3, 2)
        };

        AnsiConsole.Write(infoPanel);
        AnsiConsole.WriteLine();

        // Create selection options
        var choices = new List<string>
        {
            "Yes, unmount all",
            "Yes, let me choose which ones",
            "No, leave them mounted",
            "No, and don't ask me again"
        };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]What would you like to do?[/]")
                .AddChoices(choices)
        );

        // Handle "don't ask again" preference
        if (choice == "No, and don't ask me again")
        {
            settings.PromptToUnmountOnExit = false;
            await settingsRepository.SaveAsync(settings);
            AnsiConsole.Write(StyleGuide.Success("Preference saved. You won't be asked about unmounting on exit."));
            AnsiConsole.WriteLine();
            await Task.Delay(1500); // Brief pause to show message
            return null;
        }

        if (choice == "No, leave them mounted")
        {
            return null;
        }

        if (choice == "Yes, unmount all")
        {
            return mountedDrives;
        }

        // Let user choose which drives to unmount
        return ShowDriveSelectionPrompt(mountedDrives);
    }

    /// <summary>
    /// Shows multi-select prompt for choosing drives to unmount
    /// </summary>
    private static List<string> ShowDriveSelectionPrompt(List<string> mountedDrives)
    {
        AnsiConsole.WriteLine();

        var selectedDrives = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]Select drives to unmount:[/]")
                .Required(false)
                .PageSize(10)
                .MoreChoicesText($"[{StyleGuide.Muted}](Move up and down to see more drives)[/]")
                .InstructionsText($"[{StyleGuide.Muted}](Press [{StyleGuide.Primary}]space[/] to toggle, [{StyleGuide.Primary}]enter[/] to confirm)[/]")
                .AddChoices(mountedDrives)
        );

        return selectedDrives.ToList();
    }

    /// <summary>
    /// Performs unmount operations for the selected drives
    /// Returns number of successfully unmounted drives
    /// </summary>
    public static async Task<int> PerformUnmountAsync(
        List<string> drivesToUnmount,
        MountService mountService)
    {
        if (drivesToUnmount.Count == 0)
        {
            return 0;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Unmounting {drivesToUnmount.Count} drive(s)...[/]");
        AnsiConsole.WriteLine();

        int successCount = 0;
        int failureCount = 0;

        foreach (var mountPoint in drivesToUnmount)
        {
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Unmounting {mountPoint}...", async ctx =>
                {
                    return await mountService.UnmountDriveAsync(mountPoint);
                });

            if (result.Success)
            {
                AnsiConsole.Write(StyleGuide.Success($"Unmounted {mountPoint}"));
                AnsiConsole.WriteLine();
                successCount++;
            }
            else
            {
                AnsiConsole.Write(StyleGuide.Error($"Failed to unmount {mountPoint}: {result.ErrorMessage}"));
                AnsiConsole.WriteLine();
                failureCount++;
            }
        }

        AnsiConsole.WriteLine();

        // Show summary
        if (successCount > 0 && failureCount == 0)
        {
            AnsiConsole.Write(StyleGuide.Success($"Successfully unmounted all {successCount} drive(s)"));
        }
        else if (successCount > 0 && failureCount > 0)
        {
            AnsiConsole.Write(StyleGuide.Warning($"Unmounted {successCount} drive(s), {failureCount} failed"));
        }
        else if (failureCount > 0)
        {
            AnsiConsole.Write(StyleGuide.Error($"Failed to unmount {failureCount} drive(s)"));
        }

        AnsiConsole.WriteLine();
        await Task.Delay(2000); // Pause to show results

        return successCount;
    }
}
