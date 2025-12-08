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

