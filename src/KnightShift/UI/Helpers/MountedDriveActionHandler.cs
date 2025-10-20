using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles user actions for already-mounted drives
/// </summary>
public static class MountedDriveActionHandler
{
    public enum ActionResult
    {
        BrowseFiles,
        GoBack,
        Unmounted
    }

    /// <summary>
    /// Shows action menu for mounted drive and handles user choice
    /// Returns the action result and optional path for browsing
    /// </summary>
    public static async Task<(ActionResult Result, string? Path)> HandleAsync(
        DetectedDrive selectedDrive,
        MountService mountService)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StyleGuide.Info($"Drive {selectedDrive.DeviceName} is already mounted at {selectedDrive.MountPoint}"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]What would you like to do?[/]")
                .AddChoices(new[]
                {
                    "Browse files",
                    "Unmount drive",
                    "← Go back to drive list"
                })
        );

        if (action == "Unmount drive")
        {
            return await HandleUnmountAsync(selectedDrive, mountService);
        }
        else if (action == "← Go back to drive list")
        {
            return (ActionResult.GoBack, null);
        }
        else // Browse files
        {
            return (ActionResult.BrowseFiles, selectedDrive.MountPoint);
        }
    }

    private static async Task<(ActionResult Result, string? Path)> HandleUnmountAsync(
        DetectedDrive selectedDrive,
        MountService mountService)
    {
        // Confirm unmount
        AnsiConsole.WriteLine();
        var confirmUnmount = AnsiConsole.Confirm(
            $"[{StyleGuide.PrimaryColor}]Are you sure you want to unmount {selectedDrive.DeviceName}?[/]",
            defaultValue: false
        );

        if (!confirmUnmount)
        {
            AnsiConsole.Write(StyleGuide.Info("Unmount cancelled."));
            AnsiConsole.WriteLine();
            Task.Delay(1000).Wait();
            return (ActionResult.GoBack, null);
        }

        AnsiConsole.WriteLine();
        var unmountResult = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Unmounting {selectedDrive.DeviceName}...", async ctx =>
            {
                return await mountService.UnmountDriveAsync(selectedDrive.MountPoint!);
            });

        AnsiConsole.WriteLine();

        if (unmountResult.Success)
        {
            AnsiConsole.Write(StyleGuide.Success($"Successfully unmounted {selectedDrive.DeviceName}"));
            AnsiConsole.WriteLine();
            return (ActionResult.Unmounted, null);
        }
        else
        {
            AnsiConsole.Write(StyleGuide.Error($"Failed to unmount: {unmountResult.ErrorMessage}"));
            AnsiConsole.WriteLine();
            return (ActionResult.GoBack, null);
        }
    }
}
