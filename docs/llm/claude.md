a particular web host disallows this path
Application '/LM/W3SVC/1792/ROOT' with physical root 'D:\Sites\site36641\wwwroot\' hit unexpected managed exception, exception code = '0xe0434352'. First 30KB characters of captured stdout and stderr logs:
[Telemetry] Writing to: C:\windows\system32\config\systemprofile\AppData\Local\HelloAspDotnetTen\telemetry
[Telemetry] Run ID: 20251208_171001
Unhandled exception. System.UnauthorizedAccessException: Access to the path 'C:\windows\system32\config\systemprofile' is denied.
   at System.IO.FileSystem.CreateDirectory(String fullPath, Byte[] securityDescriptor)
   at System.IO.Directory.CreateDirectory(String path)
   at BlazorApp.Exporters.FileActivityExporter.EnsureDirectoryExists() in C:\Users\kushal\source\repos\HelloAspDotnetTen\source\HelloAspDotnetTen\BlazorApp\Exporters\FileActivityExporter.cs:line 134
   at BlazorApp.Exporters.FileActivityExporter..ctor(FileExporterOptions options) in C:\Users\kushal\source\repos\HelloAspDotnetTen\source\HelloAspDotnetTen\BlazorApp\Exporters\FileActivityExporter.cs:line 33
   at BlazorApp.Exporters.FileExporterExtensions.AddFileExporter(TracerProviderBuilder builder, FileExporterOptions options) in C:\Users\kushal\source\repos\HelloAspDotnetTen\source\HelloAspDotnetTen\BlazorApp\Exporters\FileExporterExtensions.cs:line 22
   at Program.&lt;&gt;c__DisplayClass0_0.&lt;&lt;Main&gt;$&gt;b__1(TracerProviderBuilder tracing) in C:\Users\kushal\source\repos\HelloAspDotnetTen\source\HelloAspDotnetTen\BlazorApp\Program.cs:line 32
   at OpenTelemetry.OpenTelemetryBuilderSdkExtensions.WithTracing(IOpenTelemetryBuilder builder, Action`1 configure)
   at Program.&lt;Main&gt;$(String[] args) in C:\Users\kushal\source\repos\HelloAspDotnetTen\source\HelloAspDotnetTen\BlazorApp\Program.cs:line 25
   at Program.&lt;Main&gt;(String[] args)
I think we will need to check if the path we chose is allowed and if it is not, we should probably put logs in the current directory wherever the bin is? 
the full code context is in project files in dump 
```csharp
namespace BlazorApp.Exporters;

/// <summary>
/// Configuration options for file-based OpenTelemetry exporters.
/// </summary>
public class FileExporterOptions
{
    /// <summary>
    /// The directory where telemetry files will be written.
    /// If not specified, uses the XDG data directory pattern:
    /// - Windows: %LOCALAPPDATA%/HelloAspDotnetTen/telemetry
    /// - Linux: ~/.local/share/HelloAspDotnetTen/telemetry
    /// - macOS: ~/Library/Application Support/HelloAspDotnetTen/telemetry
    /// </summary>
    public string Directory { get; set; } = GetDefaultDirectory();

    /// <summary>
    /// Maximum file size in bytes before rotation occurs.
    /// Default is 25MB (25 * 1024 * 1024 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25MB

    /// <summary>
    /// The application name used for the directory structure.
    /// </summary>
    public string ApplicationName { get; set; } = "HelloAspDotnetTen";

    /// <summary>
    /// Unique identifier for this application run.
    /// Generated at startup to ensure each run creates new files.
    /// </summary>
    public string RunId { get; set; } = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

    /// <summary>
    /// Gets the default telemetry directory following XDG/platform conventions.
    /// </summary>
    private static string GetDefaultDirectory()
    {
        string baseDir;

        if (OperatingSystem.IsWindows())
        {
            // Windows: Use LocalAppData
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // macOS: Use ~/Library/Application Support
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            // Linux/Unix: Use XDG_DATA_HOME or ~/.local/share
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            baseDir = !string.IsNullOrEmpty(xdgDataHome) 
                ? xdgDataHome 
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(baseDir, "HelloAspDotnetTen", "telemetry");
    }

    /// <summary>
    /// Creates options with default values.
    /// </summary>
    public static FileExporterOptions Default => new();

    /// <summary>
    /// Creates options for the specified directory with default 25MB size limit.
    /// </summary>
    public static FileExporterOptions ForDirectory(string directory) => new()
    {
        Directory = directory
    };

    /// <summary>
    /// Creates options for the specified directory and max file size in megabytes.
    /// </summary>
    public static FileExporterOptions Create(string directory, int maxFileSizeMb) => new()
    {
        Directory = directory,
        MaxFileSizeBytes = maxFileSizeMb * 1024L * 1024L
    };

    /// <summary>
    /// Creates options with a specific run ID (useful for testing or correlation).
    /// </summary>
    public static FileExporterOptions CreateWithRunId(string runId) => new()
    {
        RunId = runId
    };
}
```


























































































Looking at this error, the issue is clear: when running on a web host (like IIS), the `Environment.SpecialFolder.LocalApplicationData` resolves to a system profile path (`C:\windows\system32\config\systemprofile`) that the application doesn't have write access to.

Let me check the project files for the full context of how this is being used:Good, I have the full context. The problem is that `Environment.SpecialFolder.LocalApplicationData` returns a system path when running under IIS application pools. Here's an updated `GetDefaultDirectory()` method that handles this scenario with a fallback chain:The key changes:

1. **Fallback chain**: Instead of just picking one directory, it now tries multiple locations in order:
   - Platform-specific (LocalAppData, etc.) — works on dev machines
   - `AppContext.BaseDirectory/telemetry` — the app's bin folder, usually writable in hosted environments
   - `Environment.CurrentDirectory/telemetry` — current working directory
   - `Path.GetTempPath()` — temp folder as last resort

2. **`IsDirectoryWritable()` check**: Before committing to a path, it actually tests whether the directory can be created and written to.

3. **`IsSystemProtectedPath()` guard**: Proactively rejects paths under `C:\Windows`, `C:\Program Files`, etc., which are known to fail in IIS app pools.

This way, on your web host it will automatically fall back to `D:\Sites\site36641\wwwroot\telemetry` (the app base directory) or the temp folder, instead of crashing trying to write to the system profile.





```csharp
namespace BlazorApp.Exporters;

/// <summary>
/// Configuration options for file-based OpenTelemetry exporters.
/// </summary>
public class FileExporterOptions
{
    /// <summary>
    /// The directory where telemetry files will be written.
    /// Uses a fallback chain to find a writable directory:
    /// 1. XDG/platform conventions (LocalAppData, etc.)
    /// 2. Application's content root directory
    /// 3. Current working directory
    /// </summary>
    public string Directory { get; set; } = GetDefaultDirectory();

    /// <summary>
    /// Maximum file size in bytes before rotation occurs.
    /// Default is 25MB (25 * 1024 * 1024 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25MB

    /// <summary>
    /// The application name used for the directory structure.
    /// </summary>
    public string ApplicationName { get; set; } = "HelloAspDotnetTen";

    /// <summary>
    /// Unique identifier for this application run.
    /// Generated at startup to ensure each run creates new files.
    /// </summary>
    public string RunId { get; set; } = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

    /// <summary>
    /// Gets the default telemetry directory with fallback for hosted environments.
    /// </summary>
    private static string GetDefaultDirectory()
    {
        // Try platform-specific directory first
        var platformDir = GetPlatformDirectory();
        if (IsDirectoryWritable(platformDir))
        {
            return platformDir;
        }

        // Fallback 1: App's base directory (where the DLLs are)
        var appBaseDir = Path.Combine(AppContext.BaseDirectory, "telemetry");
        if (IsDirectoryWritable(appBaseDir))
        {
            return appBaseDir;
        }

        // Fallback 2: Current directory
        var currentDir = Path.Combine(Environment.CurrentDirectory, "telemetry");
        if (IsDirectoryWritable(currentDir))
        {
            return currentDir;
        }

        // Fallback 3: Temp directory (almost always writable)
        var tempDir = Path.Combine(Path.GetTempPath(), "HelloAspDotnetTen", "telemetry");
        return tempDir;
    }

    /// <summary>
    /// Gets the platform-specific directory following XDG/OS conventions.
    /// </summary>
    private static string GetPlatformDirectory()
    {
        string baseDir;

        if (OperatingSystem.IsWindows())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            baseDir = !string.IsNullOrEmpty(xdgDataHome)
                ? xdgDataHome
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(baseDir, "HelloAspDotnetTen", "telemetry");
    }

    /// <summary>
    /// Checks if a directory path is writable by attempting to create it
    /// and write a test file.
    /// </summary>
    private static bool IsDirectoryWritable(string path)
    {
        try
        {
            // Check if the path looks like a system directory we shouldn't write to
            if (IsSystemProtectedPath(path))
            {
                return false;
            }

            // Try to create the directory
            System.IO.Directory.CreateDirectory(path);

            // Try to write a test file
            var testFile = Path.Combine(path, $".write_test_{Guid.NewGuid():N}");
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects paths that are typically protected system directories.
    /// </summary>
    private static bool IsSystemProtectedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        var normalizedPath = path.Replace('/', '\\').ToLowerInvariant();

        // Windows system paths that app pools typically can't write to
        var protectedPaths = new[]
        {
            @"c:\windows",
            @"c:\program files",
            @"c:\program files (x86)",
        };

        return protectedPaths.Any(p => normalizedPath.StartsWith(p));
    }

    /// <summary>
    /// Creates options with default values.
    /// </summary>
    public static FileExporterOptions Default => new();

    /// <summary>
    /// Creates options for the specified directory with default 25MB size limit.
    /// </summary>
    public static FileExporterOptions ForDirectory(string directory) => new()
    {
        Directory = directory
    };

    /// <summary>
    /// Creates options for the specified directory and max file size in megabytes.
    /// </summary>
    public static FileExporterOptions Create(string directory, int maxFileSizeMb) => new()
    {
        Directory = directory,
        MaxFileSizeBytes = maxFileSizeMb * 1024L * 1024L
    };

    /// <summary>
    /// Creates options with a specific run ID (useful for testing or correlation).
    /// </summary>
    public static FileExporterOptions CreateWithRunId(string runId) => new()
    {
        RunId = runId
    };
}
```

