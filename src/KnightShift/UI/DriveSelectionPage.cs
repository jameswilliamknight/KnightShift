using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;
using KnightShift.UI.Helpers;

namespace KnightShift.UI;

/// <summary>
/// UI page for selecting and mounting unmounted drives
/// </summary>
public class DriveSelectionPage
{
    private readonly DriveEnumerationService _driveService;
    private readonly MountService _mountService;
    private readonly ISettingsRepository _settingsRepository;

    public DriveSelectionPage(
        DriveEnumerationService driveService,
        MountService mountService,
        ISettingsRepository settingsRepository)
    {
        _driveService = driveService;
        _mountService = mountService;
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// Displays the drive selection UI and returns the mounted drive path
    /// </summary>
    public async Task<string?> ShowAsync()
    {
        // Loop to allow returning to drive selection after unmount
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(StyleGuide.CreateTitleRule("KnightShift - Drive Selection"));
            AnsiConsole.WriteLine();

            // Check sudo privileges
            var hasSudo = await AnsiConsole.Status()
                .StartAsync("Checking privileges...", async ctx => await _mountService.HasSudoPrivilegesAsync());

            if (!hasSudo)
            {
                ShowSudoRequiredMessage();
                return null;
            }

            // Get all drives (both mounted and unmounted)
            List<DetectedDrive>? drives = await AnsiConsole.Status()
                .StartAsync("Scanning for drives...", async ctx =>
                {
                    return await _driveService.GetAllDrivesAsync();
                });

            if (drives == null || drives.Count == 0)
            {
                ShowNoDrivesFoundMessage();
                return null;
            }

            // Show stage description
            ShowStageHeader(drives);

            // Handle single drive scenario
            if (drives.Count == 1)
            {
                var result = await HandleSingleDriveAsync(drives[0]);
                if (result == SingleDriveResult.Cancel)
                {
                    return null;
                }
                if (result == SingleDriveResult.Unmounted)
                {
                    continue; // Loop back to drive selection
                }
                // Otherwise, continue to mount/access the drive
            }
            else
            {
                RenderDriveSummary(drives);
            }

            // Select drive (auto-select if only one, otherwise prompt)
            DetectedDrive selectedDrive = drives.Count == 1
                ? drives[0]
                : PromptForDriveSelection(drives);

            AnsiConsole.WriteLine();

            // If drive is already mounted, proceed directly to file browser
            if (selectedDrive.IsMounted && !string.IsNullOrWhiteSpace(selectedDrive.MountPoint))
            {
                AnsiConsole.Write(StyleGuide.Info($"Drive {selectedDrive.DeviceName} is already mounted at {selectedDrive.MountPoint}"));
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();
                return selectedDrive.MountPoint;
            }

            // Delegate to DriveMountingHandler for mounting workflow
            return await DriveMountingHandler.MountDriveAsync(selectedDrive, _mountService);
        }
    }

    /// <summary>
    /// Result of single drive handling
    /// </summary>
    private enum SingleDriveResult
    {
        Continue,   // Continue with the drive
        Cancel,     // User cancelled
        Unmounted   // Drive was unmounted, return to selection
    }

    private void ShowSudoRequiredMessage()
    {
        MessageRenderer.ShowWarningAndPause(
            "This application requires sudo privileges to mount drives.\n" +
            "Please run: sudo -v"
        );
    }

    private void ShowNoDrivesFoundMessage()
    {
        MessageRenderer.ShowInfo("No drives found.");
    }

    private void ShowStageHeader(List<DetectedDrive> drives)
    {
        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Step 1: Select Drive[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Choose a drive to access. Drives marked with ✅ are already mounted and accessible.[/]");
        AnsiConsole.WriteLine();
    }

    private async Task<SingleDriveResult> HandleSingleDriveAsync(DetectedDrive drive)
    {
        var settings = await _settingsRepository.LoadAsync();

        if (settings.SkipSingleDriveConfirmation)
        {
            return SingleDriveResult.Continue;
        }

        var driveStatusText = drive.IsMounted ? "drive (already mounted)" : "drive";
        AnsiConsole.MarkupLine($"Found [bold]1[/] {driveStatusText}:");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  {Markup.Escape(drive.DisplayString)}");
        AnsiConsole.WriteLine();

        var actionText = drive.IsMounted ? "access this drive" : "mount this drive";

        // Build choices based on mount status
        var choices = new List<string>
        {
            $"Yes, {actionText}",
            "No, cancel",
            "Yes, and don't ask again"
        };

        // Add unmount option if drive is already mounted
        if (drive.IsMounted)
        {
            choices.Insert(1, "Unmount and choose another drive");
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]There's only one option. Would you like to continue?[/]")
                .AddChoices(choices)
        );

        if (choice == "No, cancel")
        {
            AnsiConsole.Write(StyleGuide.Info("Operation cancelled."));
            AnsiConsole.WriteLine();
            return SingleDriveResult.Cancel;
        }

        if (choice == "Unmount and choose another drive")
        {
            await UnmountDriveAsync(drive);
            return SingleDriveResult.Unmounted;
        }

        if (choice == "Yes, and don't ask again")
        {
            settings.SkipSingleDriveConfirmation = true;
            await _settingsRepository.SaveAsync(settings);
            AnsiConsole.Write(StyleGuide.Success("Preference saved. You won't be asked again for single drives."));
            AnsiConsole.WriteLine();
        }

        AnsiConsole.WriteLine();
        return SingleDriveResult.Continue;
    }

    /// <summary>
    /// Unmounts a drive and shows the result
    /// </summary>
    private async Task UnmountDriveAsync(DetectedDrive drive)
    {
        if (string.IsNullOrWhiteSpace(drive.MountPoint))
        {
            MessageRenderer.ShowErrorAndPause("Cannot unmount: Mount point is unknown.");
            return;
        }

        var confirmed = AnsiConsole.Confirm(
            $"[{StyleGuide.WarningColor}]Are you sure you want to unmount {drive.DeviceName} from {drive.MountPoint}?[/]",
            defaultValue: false
        );

        if (!confirmed)
        {
            MessageRenderer.ShowInfo("Unmount cancelled.");
            return;
        }

        AnsiConsole.WriteLine();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Unmounting {drive.MountPoint}...", async ctx =>
            {
                return await _mountService.UnmountDriveAsync(drive.MountPoint);
            });

        AnsiConsole.WriteLine();

        if (result.Success)
        {
            MessageRenderer.ShowSuccessAndPause($"Successfully unmounted {drive.DeviceName} from {drive.MountPoint}");
        }
        else
        {
            MessageRenderer.ShowErrorAndPause($"Failed to unmount: {result.ErrorMessage}");
        }
    }

    private void RenderDriveSummary(List<DetectedDrive> drives)
    {
        var mountedCount = drives.Count(d => d.IsMounted);
        var unmountedCount = drives.Count - mountedCount;
        DriveSelectionHelper.RenderDriveCountSummary(drives.Count, mountedCount, unmountedCount);
    }

    private DetectedDrive PromptForDriveSelection(List<DetectedDrive> drives)
    {
        var hasMountedDrives = drives.Any(d => d.IsMounted);
        var promptTitle = hasMountedDrives
            ? $"[{StyleGuide.PrimaryColor}]Select a drive (ENTER to proceed, or select a mounted drive to see unmount option):[/]"
            : $"[{StyleGuide.PrimaryColor}]Select a drive to mount:[/]";

        return AnsiConsole.Prompt(
            new SelectionPrompt<DetectedDrive>()
                .Title(promptTitle)
                .PageSize(10)
                .MoreChoicesText($"[{StyleGuide.MutedColor}](Move up and down to see more drives)[/]")
                .AddChoices(drives)
                .UseConverter(d => Markup.Escape(d.DisplayString))
        );
    }

}
