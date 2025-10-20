using System.Diagnostics;

namespace KnightShift.Services;

/// <summary>
/// Provider for executing command-line operations using System.Diagnostics.Process
/// </summary>
public class CommandLineProvider : ICommandLineProvider
{
    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string command, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return new CommandResult
            {
                ExitCode = -1,
                Error = "Failed to start process"
            };
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error
        };
    }

    /// <inheritdoc/>
    public async Task<string> ExecuteAndGetOutputAsync(string command, string arguments)
    {
        var result = await ExecuteAsync(command, arguments);
        return result.Success ? result.Output : string.Empty;
    }
}
