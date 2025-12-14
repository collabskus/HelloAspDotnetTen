# HelloAspDotnetTen

A modern .NET 10 Blazor interactive web application featuring educational comparison games with comprehensive OpenTelemetry instrumentation for tracing, metrics, and logging.

## Features

### Interactive Game Components

**Counter** - A fun incremental counter game with win/loss mechanics
- Click to increment by 1 (simple clicks)
- Click "Slowly Increment" to generate a random value (100-900) and animate counting up
- Win if the value exceeds 500, lose otherwise
- View historical results with running totals
- Real-time win/loss tracking

**State Comparison** - Compare U.S. states across multiple dimensions
- Currently supports two question types:
  - **Area**: Which state is larger by square miles?
  - **Population**: Which state has more people?
- Randomly selects two different states per question
- Tracks your score across questions
- Extensible architecture for adding more comparison types

**Country Comparison** - Compare countries worldwide across multiple metrics
- **Population**, **GDP**, **Life Expectancy**, **Literacy Rate**, **CO2 Emissions**
- Intelligent data filtering (countries with missing data are excluded per question)
- Real-time score tracking
- Badge system shows how many countries are available for each question type
- Questions with fewer than 2 countries are automatically disabled
- Data sources: World Bank, UN, IMF, UNDP (2023/2024 estimates)

### OpenTelemetry Instrumentation

Full observability across all game components with:

**Distributed Tracing** - Track user interactions as spans with parent-child relationships
- Captures game actions (clicks, comparisons, results)
- Records game state changes and animations
- Hierarchical span structure for complex operations

**Metrics** - Understand usage patterns and performance
- Per-component counters (clicks, comparisons, correct/incorrect answers)
- Histograms for latency and value distributions
- UpDownCounters for real-time state (current counter value, session score)

**Structured Logging** - Detailed activity logs with context
- Game events and decisions
- Score updates and results
- Performance timing information

**File Export** - Persistent telemetry storage
- Automatic file rotation at configurable size limits
- Cross-platform directory support (Windows, Linux, macOS)
- Graceful fallback to bin directory if default paths are inaccessible

## Project Structure

```
HelloAspDotnetTen/
├── source/
│   └── HelloAspDotnetTen/
│       ├── BlazorApp/                 # Main Blazor interactive app
│       │   ├── Components/
│       │   │   ├── Counter.razor      # Counter game component
│       │   │   ├── StateCompare.razor # State comparison component
│       │   │   └── CountryCompare.razor # Country comparison component
│       │   ├── Exporters/
│       │   │   ├── FileActivityExporter.cs    # OTEL span exporter
│       │   │   ├── FileMetricExporter.cs      # OTEL metrics exporter
│       │   │   └── FileExporterExtensions.cs  # Extension methods
│       │   ├── Models/
│       │   │   ├── ComparisonQuestion.cs
│       │   │   └── CountryData.cs
│       │   ├── Program.cs             # Configuration & DI setup
│       │   └── appsettings.json
│       └── ClassLibrary1/             # Shared utilities
│           └── ExampleInstrumentation.cs
└── docs/
    └── llm/
        └── dump.txt                  # Full code export
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022, Visual Studio Code, or Rider

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd HelloAspDotnetTen
```

2. Navigate to the Blazor app directory:
```bash
cd source/HelloAspDotnetTen/BlazorApp
```

3. Restore dependencies and run:
```bash
dotnet run
```

4. Open your browser and navigate to `https://localhost:7156` (or the port shown in the console)

### Development Workflow

Build the entire solution:
```bash
dotnet build
```

Clean the solution:
```bash
dotnet clean
```

## Technical Details

### Render Mode

All interactive components use **InteractiveServer** render mode, meaning the `@code` blocks execute on the server via SignalR. This enables:
- Full server-side processing and OpenTelemetry instrumentation
- Direct access to injected services (ILogger, ActivitySource, Meter)
- Reliable state management with `StateHasChanged()`

### OpenTelemetry Setup

Components are instrumented with:

**ActivitySource** - For distributed tracing
```csharp
private static readonly ActivitySource ActivitySource = 
    new("BlazorApp.ComponentName");
```

**Meter** - For metrics collection
```csharp
private static readonly Meter Meter = 
    new("BlazorApp.ComponentName");
```

Activities and metrics are registered in `Program.cs`:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("BlazorApp.Counter")
        .AddSource("BlazorApp.StateCompare")
        .AddSource("BlazorApp.CountryCompare")
        .AddFileExporter(...)
    )
    .WithMetrics(metrics => metrics
        .AddMeter("BlazorApp.Counter")
        .AddMeter("BlazorApp.StateCompare")
        .AddMeter("BlazorApp.CountryCompare")
        .AddFileExporter(...)
    );
```

### Telemetry Output

Telemetry is exported to files with automatic fallback:

**Primary path** (Windows): `%LOCALAPPDATA%\HelloAspDotnetTen\telemetry\`
**Fallback path**: `{ApplicationBasePath}\bin\telemetry\`

Files are rotated when they exceed the configured size limit (default: 25MB).

## Extending the Project

### Adding a New Comparison Question Type

To add a new question to State or Country Comparison:

1. **Add data** to the respective model (e.g., `CountryData.cs`):
```csharp
public double? NewMetric { get; set; }
```

2. **Create question method** in the component:
```csharp
private ComparisonQuestion GenerateNewMetricQuestion()
{
    var (state1, state2) = SelectTwoRandomStates();
    var isCorrect = state1.NewMetric > state2.NewMetric;
    
    return new ComparisonQuestion
    {
        Question = "Which has more X?",
        LeftOption = state1.Name,
        RightOption = state2.Name,
        CorrectAnswer = isCorrect ? "left" : "right",
        LeftValue = state1.NewMetric,
        RightValue = state2.NewMetric
    };
}
```

3. **Add telemetry** for the new question type:
```csharp
var questionCounter = meter.CreateCounter<long>(
    "comparison.new_metric_attempts");
questionCounter.Add(1, new KeyValuePair<string, object?>(
    "question_type", "new_metric"));
```

4. **Update UI** to include the new button and metrics display

### Adding OpenTelemetry to a New Component

1. Add `ActivitySource` and `Meter` static fields
2. Register them in `Program.cs`
3. Create activities around key operations:
```csharp
using (var activity = ActivitySource.StartActivity("OperationName"))
{
    // Do work
    activity?.SetTag("result", result);
}
```

4. Record metrics:
```csharp
counter.Add(1, new KeyValuePair<string, object?>("tag_name", tagValue));
```

## Performance & Reliability

### State Management

The application uses Blazor component state for session-level storage (no database required):
- Score tracking is in-memory per session
- Automatically reset when the user navigates away
- Suitable for educational/demo environments

### Telemetry Performance

- File exports run asynchronously to avoid blocking UI
- Large metric batches are efficiently compressed
- File rotation prevents unlimited disk growth

### Cross-Platform Support

The file exporter uses XDG directory standards with automatic fallback:
- **Windows**: `%LOCALAPPDATA%` or bin directory
- **Linux**: `~/.local/share/` or bin directory
- **macOS**: `~/Library/Application Support/` or bin directory

Gracefully handles permission errors by falling back to the application's bin directory.

## Troubleshooting

**Telemetry files not appearing**: Check that the directory is writable. The application will fall back to the bin/telemetry directory if the default path is inaccessible.

**Component not updating**: Ensure `await InvokeAsync(StateHasChanged())` is called after async operations in event handlers.

**Missing observations**: Verify the ActivitySource and Meter names match exactly in both the component and `Program.cs` registration.

## Contributing

This project is structured for educational purposes and serves as a reference implementation for:
- Blazor interactive components with server-side rendering
- OpenTelemetry instrumentation best practices
- Cross-platform .NET development patterns

## License

GNU AFFERO GENERAL PUBLIC LICENSE Version 3

## Support

For issues or questions, please refer to the project documentation or create an issue in the repository.


---
*Notice: This project contains code generated by Large Language Models such as Claude and Gemini. All code is experimental whether explicitly stated or not.*
