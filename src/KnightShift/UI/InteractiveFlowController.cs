using Spectre.Console;
using KnightShift.Services;

namespace KnightShift.UI;

/// <summary>
/// Orchestrates the interactive TUI flow
/// </summary>
public class InteractiveFlowController
{
    private readonly DriveSelectionPage _driveSelectionPage;
    private readonly FileBrowserPage _fileBrowserPage;

    public InteractiveFlowController(
        DriveSelectionPage driveSelectionPage,
        FileBrowserPage fileBrowserPage)
    {
        _driveSelectionPage = driveSelectionPage;
        _fileBrowserPage = fileBrowserPage;
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
                    // User cancelled or unmounted - show goodbye
                    ShowGoodbye(success: false);
                    return;
                }

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
                    // Exit
                    ShowGoodbye(success: true);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(StyleGuide.Error($"An error occurred: {ex.Message}"));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Press any key to exit...[/]");
            Console.ReadKey(true);
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
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        AnsiConsole.WriteLine();
    }

    private void ShowGoodbye(bool success)
    {
        AnsiConsole.Clear();

        if (success)
        {
            var goodbyePanel = new Panel(
                new Markup(
                    $"[{StyleGuide.Success}]Thank you for using KnightShift![/]\n\n" +
                    $"Your session has ended successfully.\n\n" +
                    $"[{StyleGuide.Muted}]Run with --interactive or -i to start again.[/]"
                )
            )
            {
                Header = new PanelHeader($"{StyleGuide.CheckMark} Session Complete", Justify.Center),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(2, 1)
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

        // Create controller
        return new InteractiveFlowController(driveSelectionPage, fileBrowserPage);
    }
}
