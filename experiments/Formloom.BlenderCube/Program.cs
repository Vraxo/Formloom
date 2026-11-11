using System.Diagnostics;

namespace Formloom.BlenderCube;

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = ParseOptions(args);
        var blender = options.GetValueOrDefault("blender", "blender");
        var outputDirectory = Path.GetFullPath(options.GetValueOrDefault("output-dir", "artifacts/blender-cube"));
        var resolution = ParsePositiveInteger(options, "resolution", 512);
        var samples = ParsePositiveInteger(options, "samples", 32);
        if (samples > 256)
        {
            throw new ArgumentException("Option --samples must be between 1 and 256.");
        }

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "render_cube.py");

        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"Blender scene script was not found: {scriptPath}");
            return 1;
        }

        Directory.CreateDirectory(outputDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = blender,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("--background");
        startInfo.ArgumentList.Add("--factory-startup");
        startInfo.ArgumentList.Add("--python");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--output-dir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("--resolution");
        startInfo.ArgumentList.Add(resolution.ToString());
        startInfo.ArgumentList.Add("--samples");
        startInfo.ArgumentList.Add(samples.ToString());

        Console.WriteLine($"Blender executable: {blender}");
        Console.WriteLine($"Output directory: {outputDirectory}");
        Console.WriteLine($"Resolution: {resolution} × {resolution}; Cycles CPU samples: {samples}");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("Blender could not be started.");
            return 1;
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"Blender exited with code {process.ExitCode}.");
            return process.ExitCode;
        }

        var manifestPath = Path.Combine(outputDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine("Blender exited successfully but did not produce a render manifest.");
            return 1;
        }

        Console.WriteLine($"Render experiment completed. Manifest: {manifestPath}");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException($"Expected an option followed by a value: {argument}");
            }

            options[argument[2..]] = args[++index];
        }

        return options;
    }

    private static int ParsePositiveInteger(IReadOnlyDictionary<string, string> options, string name, int defaultValue)
    {
        if (!options.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out var parsed) || parsed < 1)
        {
            throw new ArgumentException($"Option --{name} must be a positive integer.");
        }

        return parsed;
    }
}
