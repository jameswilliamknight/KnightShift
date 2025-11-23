using Spectre.Console;
using Spectre.Console.Rendering;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Common message rendering patterns to avoid repetition
/// Eliminates the "Press any key" anti-pattern per AGENTS.md
/// </summary>
public static class MessageRenderer
{
    /// <summary>
    /// Shows a message with proper spacing (before and after)
    /// </summary>
    public static void ShowMessageWithSpacing(IRenderable message)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(message);
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows an error message and pauses briefly for user to read
    /// NO "Press any key" - follows seamless flow principle
    /// </summary>
    public static void ShowErrorAndPause(string errorMessage, int pauseMs = 3000)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StyleGuide.Error(errorMessage));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Brief pause to allow user to read the error
        Thread.Sleep(pauseMs);
    }

    /// <summary>
    /// Shows a success message and pauses briefly
    /// NO "Press any key" - follows seamless flow principle
    /// </summary>
    public static void ShowSuccessAndPause(string successMessage, int pauseMs = 1500)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StyleGuide.Success(successMessage));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Brief pause to show success
        Thread.Sleep(pauseMs);
    }

    /// <summary>
    /// Shows a warning message and pauses briefly
    /// </summary>
    public static void ShowWarningAndPause(string warningMessage, int pauseMs = 2000)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StyleGuide.Warning(warningMessage));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Brief pause to show warning
        Thread.Sleep(pauseMs);
    }

    /// <summary>
    /// Shows an info message and returns immediately
    /// </summary>
    public static void ShowInfo(string infoMessage)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StyleGuide.Info(infoMessage));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows a message without any pausing - for seamless flow
    /// </summary>
    public static void ShowMessageNoWait(IRenderable message)
    {
        AnsiConsole.Write(message);
        AnsiConsole.WriteLine();
    }
}
