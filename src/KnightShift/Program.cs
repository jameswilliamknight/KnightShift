using CommandLine;
using Spectre.Console;
using KnightShift.Commands;
using KnightShift.Services;
using KnightShift.UI;

namespace KnightShift;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // If no args provided or -i/--interactive flag, run interactive mode
        if (args.Length == 0 || args.Contains("-i") || args.Contains("--interactive"))
        {
            return await RunInteractiveMode();
        }

        // Parse command line arguments
        return await Parser.Default.ParseArguments<InteractiveOptions, MountOptions, RenameOptions, StatsOptions>(args)
            .MapResult(
                (InteractiveOptions opts) => RunInteractiveMode(),
                (MountOptions opts) => RunMountCommand(opts),
                (RenameOptions opts) => RunRenameCommand(opts),
                (StatsOptions opts) => RunStatsCommand(opts),
                errs => Task.FromResult(1)
            );
    }

    static async Task<int> RunInteractiveMode()
    {
        try
        {
            var controller = InteractiveFlowController.Create();
            await controller.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static async Task<int> RunMountCommand(MountOptions opts)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold]Mounting drive:[/] {opts.Device}");

            var mountService = new MountService();
            var result = await mountService.MountDriveAsync(opts.Device, opts.MountPath, opts.FileSystemType);

            if (result.Success)
            {
                AnsiConsole.Write(StyleGuide.Success($"Successfully mounted to {result.MountPoint}"));
                AnsiConsole.WriteLine();
                return 0;
            }
            else
            {
                AnsiConsole.Write(StyleGuide.Error($"Mount failed: {result.ErrorMessage}"));
                AnsiConsole.WriteLine();
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static async Task<int> RunRenameCommand(RenameOptions opts)
    {
        try
        {
            if (!Directory.Exists(opts.Path))
            {
                AnsiConsole.Write(StyleGuide.Error($"Directory not found: {opts.Path}"));
                AnsiConsole.WriteLine();
                return 1;
            }

            var browserService = new FileSystemBrowserService();
            var renameService = new FolderRenameService(browserService);

            // Generate preview using regex
            var previews = renameService.GenerateRenamePreview(
                opts.Path,
                opts.SearchPattern,
                opts.Replacement ?? "",
                useRegex: true
            );

            if (previews.Count == 0)
            {
                AnsiConsole.Write(StyleGuide.Info("No folders found in the specified directory."));
                AnsiConsole.WriteLine();
                return 0;
            }

            var willChange = previews.Count(p => p.WillChange);
            if (willChange == 0)
            {
                AnsiConsole.Write(StyleGuide.Info("No folders will be renamed (pattern not found)."));
                AnsiConsole.WriteLine();
                return 0;
            }

            // Show preview
            AnsiConsole.MarkupLine($"[bold]Preview:[/] {willChange} folder(s) will be renamed");
            AnsiConsole.MarkupLine($"[bold]Search:[/] {opts.SearchPattern}");
            AnsiConsole.MarkupLine($"[bold]Replace:[/] {opts.Replacement ?? "(empty)"}");
            AnsiConsole.WriteLine();

            foreach (var preview in previews.Where(p => p.WillChange).Take(10))
            {
                AnsiConsole.MarkupLine($"  {preview.OriginalName} → [green]{preview.NewName}[/]");
            }

            if (previews.Count(p => p.WillChange) > 10)
            {
                AnsiConsole.MarkupLine($"  ... and {previews.Count(p => p.WillChange) - 10} more");
            }

            AnsiConsole.WriteLine();

            // Confirm if not skipped
            if (!opts.SkipConfirmation)
            {
                var confirm = AnsiConsole.Confirm("Apply these changes?", defaultValue: false);
                if (!confirm)
                {
                    AnsiConsole.Write(StyleGuide.Info("Changes cancelled."));
                    AnsiConsole.WriteLine();
                    return 0;
                }
            }

            // Apply renames
            var result = await renameService.ApplyRenamesAsync(previews);

            if (result.Successful > 0)
            {
                AnsiConsole.Write(StyleGuide.Success($"Successfully renamed {result.Successful} folder(s)."));
                AnsiConsole.WriteLine();
            }

            if (result.HasErrors)
            {
                AnsiConsole.Write(StyleGuide.Error($"Failed to rename {result.Failed} folder(s)."));
                AnsiConsole.WriteLine();
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static async Task<int> RunStatsCommand(StatsOptions opts)
    {
        try
        {
            if (!Directory.Exists(opts.Path))
            {
                AnsiConsole.Write(StyleGuide.Error($"Directory not found: {opts.Path}"));
                AnsiConsole.WriteLine();
                return 1;
            }

            var browserService = new FileSystemBrowserService();

            AnsiConsole.MarkupLine($"[bold]Analyzing:[/] {opts.Path}");
            AnsiConsole.WriteLine();

            var size = await AnsiConsole.Status()
                .StartAsync("Calculating size...", async ctx =>
                {
                    return await Task.Run(() => browserService.CalculateDirectorySize(opts.Path));
                });

            var (fileCount, folderCount) = await Task.Run(() => browserService.CountContents(opts.Path, recursive: true));

            AnsiConsole.MarkupLine($"[bold]Size:[/] {FormatBytes(size)}");
            AnsiConsole.MarkupLine($"[bold]Files:[/] {fileCount:N0}");
            AnsiConsole.MarkupLine($"[bold]Folders:[/] {folderCount:N0}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
