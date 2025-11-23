using Spectre.Console;
using KnightShift.Services;
using KnightShift.UI.Helpers;

namespace KnightShift.UI;

/// <summary>
/// Orchestrates the interactive TUI flow
/// </summary>
public class InteractiveFlowController
{
    private readonly DriveSelectionPage _driveSelectionPage;
    private readonly FileBrowserPage _fileBrowserPage;
    private readonly MountService _mountService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly List<string> _sessionMountedDrives = new();

    public InteractiveFlowController(
        DriveSelectionPage driveSelectionPage,
        FileBrowserPage fileBrowserPage,
        MountService mountService,
        ISettingsRepository settingsRepository)
    {
        _driveSelectionPage = driveSelectionPage;
        _fileBrowserPage = fileBrowserPage;
        _mountService = mountService;
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// Starts the interactive flow
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            ShowWelcome();

            // Main loop - allows user to unmount and re-select drives
            while (true)
            {
                // Step 1: Select and mount drive
                var mountPoint = await _driveSelectionPage.ShowAsync();

                if (mountPoint == null)
                {
                    // User cancelled or unmounted - show goodbye with unmount prompt
                    await ShowGoodbyeWithUnmountCheckAsync(success: false);
                    return;
                }

                // Track if this is a newly mounted drive in /mnt/*
                TrackSessionMount(mountPoint);

                // Step 2: Browse mounted drive
                await _fileBrowserPage.ShowAsync(mountPoint);

                // After browsing, ask if user wants to continue or exit
                AnsiConsole.Clear();
                var continueSession = AnsiConsole.Confirm(
                    $"[{StyleGuide.PrimaryColor}]Would you like to select another drive?[/]",
                    defaultValue: false
                );

                if (!continueSession)
                {
                    // Exit with unmount prompt
                    await ShowGoodbyeWithUnmountCheckAsync(success: true);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.Clear();
            MessageRenderer.ShowErrorAndPause($"An error occurred: {ex.Message}");
        }
    }

    private void ShowWelcome()
    {
        AnsiConsole.Clear();

        var welcomePanel = new Panel(
            new Markup(
                $"[{StyleGuide.Primary}]Welcome to KnightShift![/]\n\n" +
                $"This tool helps you:\n" +
                $"  {StyleGuide.Bullet} Mount unmounted drives in WSL\n" +
                $"  {StyleGuide.Bullet} Browse your files and folders\n" +
                $"  {StyleGuide.Bullet} Batch rename folders by removing text\n" +
                $"  {StyleGuide.Bullet} View folder properties and statistics\n\n" +
                $"[{StyleGuide.Muted}]Let's get started![/]"
            )
        )
        {
            Header = new PanelHeader($"{StyleGuide.USB} KnightShift", Justify.Center),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.DodgerBlue1),
            Padding = new Padding(3, 2)
        };

        AnsiConsole.Write(welcomePanel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Tracks mount point if it's in /mnt/ and not already tracked
    /// </summary>
    private void TrackSessionMount(string mountPoint)
    {
        // Only track mounts in /mnt/* to avoid tracking system mounts
        if (mountPoint.StartsWith("/mnt/") && !_sessionMountedDrives.Contains(mountPoint))
        {
            _sessionMountedDrives.Add(mountPoint);
        }
    }

    /// <summary>
    /// Shows goodbye message with optional unmount prompt for session-mounted drives
    /// </summary>
    private async Task ShowGoodbyeWithUnmountCheckAsync(bool success)
    {
        // Check if we have any session-mounted drives to unmount
        if (_sessionMountedDrives.Count > 0)
        {
            var drivesToUnmount = await UI.Helpers.UnmountPromptHandler.ShowUnmountPromptAsync(
                _sessionMountedDrives,
                _settingsRepository
            );

            if (drivesToUnmount != null && drivesToUnmount.Count > 0)
            {
                await UI.Helpers.UnmountPromptHandler.PerformUnmountAsync(
                    drivesToUnmount,
                    _mountService
                );
            }
        }

        // Show goodbye message
        ShowGoodbye(success);
    }

    private void ShowGoodbye(bool success)
    {
        AnsiConsole.Clear();

        if (success)
        {
            var goodbyePanel = new Panel(
                new Markup(
                    $"[{StyleGuide.SuccessMarkup}]Thank you for using KnightShift![/]\n\n" +
                    $"Your session has ended successfully.\n\n" +
                    $"[{StyleGuide.Muted}]Run with --interactive or -i to start again.[/]"
                )
            )
            {
                Header = new PanelHeader($"{StyleGuide.CheckMark} Session Complete", Justify.Center),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(3, 2)
            };

            AnsiConsole.Write(goodbyePanel);
        }
        else
        {
            AnsiConsole.Write(StyleGuide.Info("Session ended."));
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Factory method to create a fully configured InteractiveFlowController
    /// </summary>
    public static InteractiveFlowController Create()
    {
        // Create services
        var driveService = new DriveEnumerationService();
        var mountService = new MountService();
        var browserService = new FileSystemBrowserService();
        var renameService = new FolderRenameService(browserService);
        var settingsRepository = new SettingsRepository();

        // Create UI pages
        var driveSelectionPage = new DriveSelectionPage(driveService, mountService, settingsRepository);
        var propertiesPanel = new PropertiesPanel(browserService);
        var textRemovalPage = new TextRemovalPage(renameService, browserService);
        var fileBrowserPage = new FileBrowserPage(browserService, textRemovalPage, propertiesPanel);

        // Create controller with mount service and settings for unmount tracking
        return new InteractiveFlowController(
            driveSelectionPage,
            fileBrowserPage,
            mountService,
            settingsRepository
        );
    }
}
