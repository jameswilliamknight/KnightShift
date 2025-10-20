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
            var shouldContinue = await HandleSingleDriveAsync(drives[0]);
            if (!shouldContinue)
            {
                return null;
            }
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

        // If drive is already mounted, show action menu
        if (selectedDrive.IsMounted && !string.IsNullOrWhiteSpace(selectedDrive.MountPoint))
        {
            var (result, path) = await MountedDriveActionHandler.HandleAsync(selectedDrive, _mountService);

            if (result == MountedDriveActionHandler.ActionResult.BrowseFiles)
            {
                return path;
            }
            else // GoBack or Unmounted
            {
                return null;
            }
        }

        // Prompt for mount point
        var mountPoint = PromptForMountPoint(selectedDrive);
        if (mountPoint == null)
        {
            return null;
        }

        // Show confirmation and mount
        DriveSelectionHelper.RenderMountConfirmationPanel(selectedDrive, mountPoint);

        var confirmed = AnsiConsole.Confirm(
            $"[{StyleGuide.PrimaryColor}]Proceed with mounting?[/]",
            defaultValue: true
        );

        if (!confirmed)
        {
            AnsiConsole.Write(StyleGuide.Info("Mount operation cancelled."));
            AnsiConsole.WriteLine();
            return null;
        }

        return await MountDriveAsync(selectedDrive, mountPoint);
    }

    private void ShowSudoRequiredMessage()
    {
        AnsiConsole.Write(StyleGuide.Warning(
            "This application requires sudo privileges to mount drives.\n" +
            "Please run: sudo -v"
        ));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private void ShowNoDrivesFoundMessage()
    {
        AnsiConsole.Write(StyleGuide.Info("No drives found."));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to exit...");
        Console.ReadKey(true);
    }

    private void ShowStageHeader(List<DetectedDrive> drives)
    {
        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Step 1: Select Drive[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Choose a drive to access. Drives marked with ✅ are already mounted and accessible.[/]");
        AnsiConsole.WriteLine();
    }

    private async Task<bool> HandleSingleDriveAsync(DetectedDrive drive)
    {
        var settings = await _settingsRepository.LoadAsync();

        if (settings.SkipSingleDriveConfirmation)
        {
            return true;
        }

        var driveStatusText = drive.IsMounted ? "drive (already mounted)" : "drive";
        AnsiConsole.MarkupLine($"Found [bold]1[/] {driveStatusText}:");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  {Markup.Escape(drive.DisplayString)}");
        AnsiConsole.WriteLine();

        var actionText = drive.IsMounted ? "access this drive" : "mount this drive";
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{StyleGuide.PrimaryColor}]There's only one option. Would you like to continue?[/]")
                .AddChoices(new[]
                {
                    $"Yes, {actionText}",
                    "No, cancel",
                    "Yes, and don't ask again"
                })
        );

        if (choice == "No, cancel")
        {
            AnsiConsole.Write(StyleGuide.Info("Operation cancelled."));
            AnsiConsole.WriteLine();
            return false;
        }

        if (choice == "Yes, and don't ask again")
        {
            settings.SkipSingleDriveConfirmation = true;
            await _settingsRepository.SaveAsync(settings);
            AnsiConsole.Write(StyleGuide.Success("Preference saved. You won't be asked again for single drives."));
            AnsiConsole.WriteLine();
        }

        AnsiConsole.WriteLine();
        return true;
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

    private string? PromptForMountPoint(DetectedDrive selectedDrive)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Step 2: Choose Mount Point[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]This is where the drive will be accessible in your filesystem.[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Type 'help' for more information, or press Enter to use the default.[/]");
        AnsiConsole.WriteLine();

        var defaultMountPoint = DriveSelectionHelper.GetDefaultMountPoint(selectedDrive);

        while (true)
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>($"[{StyleGuide.PrimaryColor}]Mount point:[/]")
                    .DefaultValue(defaultMountPoint)
                    .AllowEmpty()
            );

            // Handle help request
            if (input.Equals("help", StringComparison.OrdinalIgnoreCase) || input == "?")
            {
                DriveSelectionHelper.ShowMountPointHelp(defaultMountPoint);
                continue;
            }

            // Use default if empty
            if (string.IsNullOrWhiteSpace(input))
            {
                input = defaultMountPoint;
            }

            // Validate path
            var validation = MountPointValidator.Validate(input);
            if (!validation.IsValid)
            {
                AnsiConsole.MarkupLine($"[{StyleGuide.ErrorColor}]{Markup.Escape(validation.ErrorMessage)}[/]");
                AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Type 'help' for guidance.[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            return input;
        }
    }

    private async Task<string?> MountDriveAsync(DetectedDrive selectedDrive, string mountPoint)
    {
        AnsiConsole.WriteLine();

        MountService.MountResult? result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Mounting {selectedDrive.DeviceName} to {mountPoint}...", async ctx =>
            {
                return await _mountService.MountDriveAsync(
                    selectedDrive.DevicePath,
                    mountPoint,
                    selectedDrive.FileSystemType,
                    selectedDrive.IsWindowsDrive
                );
            });

        AnsiConsole.WriteLine();

        if (result.Success)
        {
            return HandleMountSuccess(result, selectedDrive);
        }
        else
        {
            HandleMountFailure(result, selectedDrive);
            return null;
        }
    }

    private string? HandleMountSuccess(MountService.MountResult result, DetectedDrive selectedDrive)
    {
        // Validate mount point - should never be null/empty on success
        if (string.IsNullOrWhiteSpace(result.MountPoint))
        {
            AnsiConsole.Write(StyleGuide.Error("Internal error: Mount succeeded but mount point is empty."));
            AnsiConsole.WriteLine();
            return null;
        }

        AnsiConsole.Write(StyleGuide.Success($"Successfully mounted {selectedDrive.DeviceName} to {result.MountPoint}"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]What's Next:[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Your drive is now accessible at:[/] [cyan]{Markup.Escape(result.MountPoint)}[/]");
        AnsiConsole.WriteLine();

        return result.MountPoint;
    }

    private void HandleMountFailure(MountService.MountResult result, DetectedDrive selectedDrive)
    {
        AnsiConsole.Write(StyleGuide.Error($"Failed to mount drive: {result.ErrorMessage}"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Common issues:[/]");
        AnsiConsole.MarkupLine($"  • Directory already exists - try a different mount point");
        AnsiConsole.MarkupLine($"  • Permission denied - ensure sudo access is configured");
        AnsiConsole.MarkupLine($"  • Drive already mounted - check with 'mount | grep {Markup.Escape(selectedDrive.DeviceName)}'");
        AnsiConsole.WriteLine();
    }
}
