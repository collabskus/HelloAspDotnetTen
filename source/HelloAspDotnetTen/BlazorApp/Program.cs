using BlazorApp.Components;
using BlazorApp.Exporters;
using ClassLibrary1;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// File exporter configuration
// Uses XDG-style directories by default:
// - Windows: %LOCALAPPDATA%/HelloAspDotnetTen/telemetry
// - Linux: ~/.local/share/HelloAspDotnetTen/telemetry
// - macOS: ~/Library/Application Support/HelloAspDotnetTen/telemetry
// Each run creates new files with timestamp in filename (never appends)
var fileExporterOptions = new FileExporterOptions
{
    MaxFileSizeBytes = 25 * 1024 * 1024 // 25MB
};

Console.WriteLine($"[Telemetry] Writing to: {fileExporterOptions.Directory}");
Console.WriteLine($"[Telemetry] Run ID: {fileExporterOptions.RunId}");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "BlazorApp",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("ClassLibrary1")
            .AddSource("BlazorApp.Counter")  // ← ADD THIS LINE
            // Console exporter for immediate visibility
            .AddConsoleExporter()
            // File exporter for persistent storage
            .AddFileExporter(fileExporterOptions);
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("ClassLibrary1")
            .AddMeter("BlazorApp.Counter")   // ← ADD THIS LINE
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            // Console exporter for immediate visibility
            .AddConsoleExporter()
            // File exporter for persistent storage (exports every 10 seconds)
            .AddFileExporter(fileExporterOptions);
    });

// Configure logging with OpenTelemetry
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: "BlazorApp",
            serviceVersion: "1.0.0"));
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    // Console exporter for immediate visibility
    options.AddConsoleExporter();
    // File exporter for persistent storage
    options.AddFileExporter(fileExporterOptions);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHelloDotnetLibrary(builder.Configuration);
// Add this line in Program.cs after the existing service registrations
// (after builder.Services.AddHelloDotnetLibrary(builder.Configuration);)

// Register StateComparisonService as Scoped so each user session maintains their own score
builder.Services.AddScoped<BlazorApp.Services.StateComparisonService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting HelloAspDotnetTen Blazor Application");
logger.LogInformation("========================================");

// Test the services
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

logger.LogInformation("--- Scenario 1: Sequential Operations ---");
for (int i = 1; i <= 3; i++)
{
    logger.LogInformation("Sequential iteration {Iteration}", i);
    Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
    Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");
    await Task.Delay(500);
}

logger.LogInformation("--- Scenario 2: Parallel Operations ---");
var tasks = Enumerable.Range(1, 5).Select(async i =>
{
    logger.LogInformation("Parallel task {TaskId} starting", i);
    await Task.Delay(100 * i);
    var len = c1.GetLengthOfInjectedProperty();
    logger.LogInformation("Parallel task {TaskId} completed with length {Length}", i, len);
    return len;
});

var results = await Task.WhenAll(tasks);
logger.LogInformation("Parallel results: {Results}", string.Join(", ", results));

logger.LogInformation("========================================");
logger.LogInformation("Application completed.");
logger.LogInformation("Telemetry files written to: {Directory}", fileExporterOptions.Directory);

Console.WriteLine($"\nTelemetry files for this run (ID: {fileExporterOptions.RunId}):");
Console.WriteLine($"Directory: {fileExporterOptions.Directory}");
if (Directory.Exists(fileExporterOptions.Directory))
{
    var runFiles = Directory.GetFiles(fileExporterOptions.Directory, $"*_{fileExporterOptions.RunId}*.json");
    foreach (var file in runFiles)
    {
        var info = new FileInfo(file);
        Console.WriteLine($"  - {info.Name} ({info.Length:N0} bytes)");
    }

    if (runFiles.Length == 0)
    {
        Console.WriteLine("  (no files created yet - telemetry may still be flushing)");
    }
}
else
{
    Console.WriteLine("  (directory not created yet)");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
