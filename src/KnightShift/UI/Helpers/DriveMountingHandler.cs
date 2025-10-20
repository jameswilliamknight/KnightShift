using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles drive mounting workflow including mount point selection and execution
/// </summary>
public static class DriveMountingHandler
{
    /// <summary>
    /// Handles the complete mounting workflow for a drive
    /// Returns the mount point if successful, null otherwise
    /// </summary>
    public static async Task<string?> MountDriveAsync(
        DetectedDrive selectedDrive,
        MountService mountService)
    {
        // Prompt for mount point
        var mountPoint = PromptForMountPoint(selectedDrive);
        if (mountPoint == null)
        {
            return null;
        }

        // Show confirmation
        RenderMountConfirmationPanel(selectedDrive, mountPoint);

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

        // Execute mount operation
        return await ExecuteMountAsync(selectedDrive, mountPoint, mountService);
    }

    /// <summary>
    /// Prompts user for mount point with validation
    /// </summary>
    private static string? PromptForMountPoint(DetectedDrive selectedDrive)
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

    /// <summary>
    /// Renders mount confirmation panel
    /// </summary>
    private static void RenderMountConfirmationPanel(DetectedDrive selectedDrive, string mountPoint)
    {
        DriveSelectionHelper.RenderMountConfirmationPanel(selectedDrive, mountPoint);
    }

    /// <summary>
    /// Executes the mount command and handles results
    /// </summary>
    private static async Task<string?> ExecuteMountAsync(
        DetectedDrive selectedDrive,
        string mountPoint,
        MountService mountService)
    {
        AnsiConsole.WriteLine();

        MountService.MountResult result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Mounting {selectedDrive.DeviceName} to {mountPoint}...", async ctx =>
            {
                return await mountService.MountDriveAsync(
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

    /// <summary>
    /// Handles successful mount operation
    /// </summary>
    private static string? HandleMountSuccess(MountService.MountResult result, DetectedDrive selectedDrive)
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

    /// <summary>
    /// Handles failed mount operation
    /// </summary>
    private static void HandleMountFailure(MountService.MountResult result, DetectedDrive selectedDrive)
    {
        AnsiConsole.Write(StyleGuide.Error($"Failed to mount drive: {result.ErrorMessage}"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Common issues:[/]");
        AnsiConsole.MarkupLine($"  • Directory already exists - try a different mount point");
        AnsiConsole.MarkupLine($"  • Permission denied - ensure sudo access is configured");
        AnsiConsole.MarkupLine($"  • Drive already mounted - check with 'mount | grep {Markup.Escape(selectedDrive.DeviceName)}'");
        AnsiConsole.WriteLine();

        // Brief pause to allow user to read the error
        Thread.Sleep(3000);
    }
}
