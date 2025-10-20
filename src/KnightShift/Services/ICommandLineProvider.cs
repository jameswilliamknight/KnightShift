namespace KnightShift.Services;

/// <summary>
/// Interface for executing command-line operations
/// </summary>
public interface ICommandLineProvider
{
    /// <summary>
    /// Executes a command and returns the result
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="arguments">Command arguments</param>
    /// <returns>Command result containing exit code, stdout, and stderr</returns>
    Task<CommandResult> ExecuteAsync(string command, string arguments);

    /// <summary>
    /// Executes a command and returns only stdout if successful
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="arguments">Command arguments</param>
    /// <returns>Stdout if successful, empty string otherwise</returns>
    Task<string> ExecuteAndGetOutputAsync(string command, string arguments);
}

/// <summary>
/// Result of a command execution
/// </summary>
public class CommandResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;
}
