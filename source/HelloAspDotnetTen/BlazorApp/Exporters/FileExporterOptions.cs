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
