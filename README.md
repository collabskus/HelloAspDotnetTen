# HelloAspDotnetTen

A modern .NET 10 Blazor interactive web application featuring educational comparison games with comprehensive OpenTelemetry instrumentation and persistent local storage. This project serves as both a learning exercise and demonstration of software best practices.

## Live Demo

Visit the deployed application at: http://open.runasp.net (hosted by MonsterASP)

## Features

### Interactive Game Components

#### Counter - Incremental Counter Game with Win/Loss Mechanics
- **Simple Increment**: Click +1 button for basic counting
- **Slow Increment ("Fair Toss")**: Generates random value (100-900) with animated counting
- **Win/Loss System**: Win if toss value exceeds 500, lose otherwise
- **Unattended Mode**: Automated continuous tossing for testing/demonstration
- **Comprehensive Statistics**:
  - Session tracking: Current count and historical tosses
  - Lifetime statistics: Total tosses, wins, losses, win percentage
  - Historical records: Highest/lowest toss values, first/last played timestamps
- **Full Persistence**: All session state and lifetime statistics survive page refreshes and application updates

#### State Comparison - U.S. States Quiz Game
- **Three Question Types**:
  - **Area**: Which state is larger by square miles?
  - **Population**: Which state has more people?
  - **Representatives**: Which state has more representatives in the House?
- **Two Game Modes**:
  - **Fixed Question**: Select a specific comparison type
  - **Random Mode**: Mix all question types for variety
- **Smart Question Generation**: Automatically filters out ties to prevent "gotcha" questions
- **Comprehensive Persistence**:
  - Session data: Current score, streak, question type, game mode
  - Lifetime statistics: Total questions, correct answers, best streak, play timestamps
- **Real-time Feedback**: Instant visual indication of correct/incorrect answers
- **Responsive Design**: Optimized for desktop and mobile (including iPhone SE 2020)

#### Country Comparison - Global Knowledge Quiz
- **Five Question Types**:
  - **Population**: Which country has more people?
  - **GDP**: Which country has a higher GDP? (formatted as $XXX,XXX million)
  - **Life Expectancy**: Which country has higher life expectancy?
  - **Literacy Rate**: Which country has a higher literacy rate?
  - **CO2 Emissions**: Which country emits more CO2?
- **Intelligent Data Filtering**:
  - Countries with missing data are excluded per question type
  - Badge system shows available country count for each question
  - Questions with fewer than 2 countries are automatically disabled
- **Smart Question Generation**: Prevents asking questions where values are equal
- **Two Game Modes**: Fixed question type or random mix
- **Full Persistence**: Session scores, streaks, and lifetime statistics
- **Global Coverage**: 195 countries with 2023/2024 data from World Bank, UN, IMF, UNDP
- **Mobile-Responsive**: Entire question fits on screen without scrolling (iPhone SE 2020 tested)

### Data Persistence

All components implement comprehensive browser local storage persistence:

**Namespace Pattern**: `HelloAspDotnetTen.{Component}.Stats`
- Counter: `HelloAspDotnetTen.Counter.Stats`
- StateCompare: `HelloAspDotnetTen.StateCompare.Stats`
- CountryCompare: `HelloAspDotnetTen.CountryCompare.Stats`

**Persistence Strategy**:
- **Immediate writes**: No pooling or batching - every change is saved immediately
- **Complete state restoration**: All session and lifetime data restored on page load
- **Survives**: Page refreshes, navigation, application updates, browser restarts
- **Graceful degradation**: Works in-memory if localStorage unavailable

**Data Stored**:
- Session state: Current scores, streaks, question types, game modes, UI state
- Lifetime statistics: Aggregate totals, best streaks, historical records, timestamps
- Component state: Current count (Counter), historical tosses (Counter)

### OpenTelemetry Instrumentation

Full observability across all game components with production-ready telemetry:

#### Distributed Tracing
Track user interactions as hierarchical spans:
- Component lifecycle: Initialize, StartNewRound, ResetGame, Dispose
- User actions: Click events, answer selections, mode toggles
- Data operations: Local storage loads/saves, state updates
- Performance timing: Slow increment animations, async operations

**Span Hierarchy Example** (Counter):
```
Counter.SlowlyIncrement (parent)
├── Counter.UpdatePersistedStats
└── Counter.SavePersistedStats
```

#### Metrics Collection
Comprehensive metrics for usage patterns and performance:

**Counters** (monotonically increasing):
- Component-specific action counts (clicks, comparisons, answers)
- Correct/incorrect answer tracking
- Local storage operation counts
- Mode changes and resets

**Histograms** (value distributions):
- Toss value ranges (Counter)
- Streak lengths when broken
- Answer latencies

**UpDownCounters** (gauges):
- Current counter value
- Current score
- Current streak
- Unattended mode status

**Example Metrics**:
```
counter.clicks: 150 {component="simple"}
counter.tosses: 45 {result="win"}
statecompare.answers: 32 {question_type="area", correct="true"}
countrycompare.current_score: 18 (gauge)
```

#### Structured Logging
Contextual logging with semantic information:
- Game events with timestamps
- Score updates with deltas
- Question generation details
- Error conditions with stack traces

#### File Export System
Persistent telemetry storage with enterprise features:

**Export Locations** (with cross-platform fallback):
- **Windows**: `%LOCALAPPDATA%\HelloAspDotnetTen\telemetry\`
- **Linux**: `~/.local/share/HelloAspDotnetTen/telemetry/`
- **macOS**: `~/Library/Application Support/HelloAspDotnetTen/telemetry/`
- **Fallback**: `{ApplicationBasePath}\bin\telemetry\`

**Features**:
- Automatic file rotation at configurable size (default: 25MB)
- Separate files for traces, metrics, and logs
- Async writes to prevent UI blocking
- Graceful handling of permission errors

## Project Structure

```
HelloAspDotnetTen/
├── .github/
│   └── workflows/
│       └── dotnet.yml                     # CI/CD: Multi-platform builds
├── source/
│   └── HelloAspDotnetTen/
│       ├── BlazorApp/                     # Main Blazor interactive app
│       │   ├── Components/
│       │   │   ├── Pages/
│       │   │   │   ├── Counter.razor      # Counter game with unattended mode
│       │   │   │   ├── StateCompare.razor # U.S. states comparison game
│       │   │   │   └── CountryCompare.razor # World countries comparison game
│       │   │   └── Layout/                # Navigation and layout components
│       │   ├── Exporters/
│       │   │   ├── FileActivityExporter.cs     # OTEL trace exporter
│       │   │   ├── FileMetricExporter.cs       # OTEL metrics exporter
│       │   │   └── FileExporterExtensions.cs   # DI extensions
│       │   ├── Models/
│       │   │   ├── ComparisonQuestion.cs       # Generic question model
│       │   │   ├── StateData.cs                # U.S. state data model
│       │   │   ├── CountryData.cs              # Country data model
│       │   │   └── GameScore.cs                # Score tracking model
│       │   ├── Services/
│       │   │   ├── StateComparisonService.cs   # State game logic
│       │   │   └── CountryComparisonService.cs # Country game logic
│       │   ├── wwwroot/
│       │   │   └── app.css                     # Mobile-responsive styles
│       │   ├── Program.cs                      # OTEL & DI configuration
│       │   └── appsettings.json
│       └── ClassLibrary1/                 # Shared utilities
│           └── ExampleInstrumentation.cs
└── docs/
    └── llm/
        └── dump.txt                       # Complete code export for AI context
```

## Getting Started

### Prerequisites

- **.NET 10 SDK** (not .NET 9 - version precision is critical)
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Modern web browser (Chrome, Firefox, Safari, Edge)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/collabskus/HelloAspDotnetTen.git
cd HelloAspDotnetTen
```

2. Navigate to the Blazor app directory:
```bash
cd source/HelloAspDotnetTen/BlazorApp
```

3. Restore dependencies and run:
```bash
dotnet restore
dotnet run
```

4. Open your browser and navigate to the HTTPS endpoint shown in the console (typically `https://localhost:7156`)

### Development Workflow

**Build the solution**:
```bash
dotnet build
```

**Run with hot reload** (from BlazorApp directory):
```bash
dotnet watch run
```

**Clean the solution**:
```bash
dotnet clean
```

**Run tests** (when added):
```bash
dotnet test
```

## Technical Architecture

### Render Mode

All interactive components use **InteractiveServer** render mode:
- Component `@code` blocks execute entirely on the server
- UI updates sent to browser via SignalR WebSocket connection
- Enables full server-side OpenTelemetry instrumentation
- Reliable state management with `StateHasChanged()`
- Direct access to injected services (ILogger, ActivitySource, Meter)

**Render Mode Declaration**:
```csharp
@rendermode InteractiveServer
```

### OpenTelemetry Configuration

Components register instrumentation in `Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("BlazorApp.Counter")
        .AddSource("BlazorApp.StateCompare")
        .AddSource("BlazorApp.CountryCompare")
        .AddFileExporter(options => {
            options.FilePath = "telemetry/traces.json";
            options.MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB
        })
    )
    .WithMetrics(metrics => metrics
        .AddMeter("BlazorApp.Counter")
        .AddMeter("BlazorApp.StateCompare")
        .AddMeter("BlazorApp.CountryCompare")
        .AddFileExporter(options => {
            options.FilePath = "telemetry/metrics.json";
            options.MaxFileSizeBytes = 25 * 1024 * 1024;
        })
    );
```

**Component Instrumentation Pattern**:
```csharp
// Static fields for OpenTelemetry
private static readonly ActivitySource ActivitySource = 
    new("BlazorApp.ComponentName", "1.0.0");
private static readonly Meter Meter = 
    new("BlazorApp.ComponentName", "1.0.0");

// Create instruments
private static readonly Counter<long> ClickCounter = 
    Meter.CreateCounter<long>("component.clicks", 
        unit: "{clicks}", 
        description: "Total clicks");

// Use in methods
using (var activity = ActivitySource.StartActivity("Operation"))
{
    ClickCounter.Add(1);
    activity?.SetTag("result", result);
}
```

### State Management

**Session State**:
- Stored in component fields
- Persisted to localStorage immediately on every change
- Restored from localStorage in `OnAfterRenderAsync(firstRender: true)`
- Survives page refreshes and navigation

**Lifetime Statistics**:
- Aggregate data across all sessions
- Updated immediately after each significant action
- Never batched or pooled to prevent data loss
- Includes timestamps for historical tracking

**Service Layer**:
- `StateComparisonService` and `CountryComparisonService`
- Registered as scoped services in DI container
- Manage game logic, question generation, answer validation
- Track session scores (separate from persisted lifetime stats)

### Local Storage Implementation

**Key Pattern**: Namespaced to prevent collisions
```javascript
localStorage.setItem("HelloAspDotnetTen.Counter.Stats", json);
```

**Save Strategy**:
```csharp
private async Task SavePersistedStatsAsync()
{
    if (!_isJsRuntimeAvailable) return;
    
    // Update all fields
    _persistedStats.SessionCorrect = ComparisonService.Score.CorrectAnswers;
    _persistedStats.TotalQuestions += 1;
    _persistedStats.LastPlayedUtc = DateTime.UtcNow;
    
    // Serialize and save immediately
    var json = JsonSerializer.Serialize(_persistedStats);
    await JSRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
}
```

**Load Strategy**:
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _isJsRuntimeAvailable = true;
        await LoadPersistedStatsAsync();
        StateHasChanged(); // Trigger UI update
    }
}
```

### Smart Question Generation

Both comparison games implement tie detection to prevent "gotcha" questions:

**State Comparison** (`StateComparisonService.cs`):
```csharp
public (StateData, StateData) GetRandomStatePairWithoutTie(ComparisonQuestion question)
{
    const int maxAttempts = 100;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        var pair = GetRandomStatePair();
        var v1 = question.GetValue(pair.Item1);
        var v2 = question.GetValue(pair.Item2);
        
        if (v1 != v2) return pair; // Found valid pair
    }
    throw new InvalidOperationException("Cannot find pair without tie");
}
```

**Country Comparison** (`CountryComparisonService.cs`):
- Pre-filters countries missing required data
- Validates minimum 2 countries available
- Retries up to 100 times to find non-equal pairs
- Throws exception if no valid pair exists (shouldn't happen with real data)

### Mobile Responsiveness

Custom CSS (`app.css`) ensures optimal mobile experience:

```css
/* Ensure full question fits on mobile screens */
.question-container {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    padding: 1rem;
}

/* Responsive button layout */
.comparison-buttons {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
}

@media (max-width: 375px) { /* iPhone SE 2020 */
    .btn {
        font-size: 0.9rem;
        padding: 0.5rem;
    }
}
```

**Testing Targets**:
- Desktop: 1920x1080 and larger
- Tablet: iPad (768x1024)
- Mobile: iPhone SE 2020 (375x667) - smallest supported screen

## Continuous Integration

GitHub Actions workflow (`.github/workflows/dotnet.yml`):

**Triggers**:
- Every commit to any branch
- Every pull request to any branch

**Platforms Tested**:
- Windows (windows-latest)
- macOS (macos-latest)
- Linux (ubuntu-latest)

**Steps**:
1. Checkout code
2. Setup .NET 10 SDK
3. Restore dependencies
4. Build solution
5. Fail build if any errors occur

**Purpose**: Verify cross-platform compatibility and catch build errors early

## Deployment

**Manual Deployment** (current process):
1. Build solution in Visual Studio (Release configuration)
2. Publish BlazorApp project
3. Deploy to MonsterASP hosting via Visual Studio publish profile
4. Verify at http://open.runasp.net

**Future Enhancements**:
- Automated deployment via GitHub Actions
- Docker containerization
- Azure App Service deployment

## Extending the Project

### Adding a New Question Type

#### For State Comparison:

1. **Add property to `StateData.cs`**:
```csharp
public int? MedianAge { get; set; }
```

2. **Add question in `StateComparisonService.cs`**:
```csharp
new ComparisonQuestion
{
    Id = "median_age",
    QuestionTemplate = "Which state has a higher median age?",
    PropertyName = nameof(StateData.MedianAge),
    GetValue = s => s.MedianAge,
    Unit = "years",
    FormatString = "{0:F1}"
}
```

3. **Update UI in `StateCompare.razor`** to show new button

4. **Add telemetry**:
```csharp
AnswersCounter.Add(1, new KeyValuePair<string, object?>("question_type", "median_age"));
```

#### For Country Comparison:

Same pattern as State Comparison, but update `CountryData.cs` and `CountryComparisonService.cs`

### Adding OpenTelemetry to a New Component

1. **Add static fields**:
```csharp
private static readonly ActivitySource ActivitySource = new("BlazorApp.NewComponent", "1.0.0");
private static readonly Meter Meter = new("BlazorApp.NewComponent", "1.0.0");
```

2. **Create instruments**:
```csharp
private static readonly Counter<long> ActionCounter = 
    Meter.CreateCounter<long>("newcomponent.actions");
```

3. **Register in `Program.cs`**:
```csharp
.AddSource("BlazorApp.NewComponent")
.AddMeter("BlazorApp.NewComponent")
```

4. **Use in code**:
```csharp
using (var activity = ActivitySource.StartActivity("NewComponent.Action"))
{
    ActionCounter.Add(1);
    // Perform action
    activity?.SetTag("result", "success");
}
```

### Adding Local Storage Persistence

1. **Define storage key constant**:
```csharp
private const string LocalStorageKey = "HelloAspDotnetTen.NewComponent.Stats";
```

2. **Create stats model**:
```csharp
private class NewComponentStats
{
    public int TotalActions { get; set; }
    public DateTime FirstPlayedUtc { get; set; }
    public DateTime LastPlayedUtc { get; set; }
}
```

3. **Implement load/save/clear methods** (follow Counter.razor pattern)

4. **Load in `OnAfterRenderAsync`**:
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _isJsRuntimeAvailable = true;
        await LoadPersistedStatsAsync();
        StateHasChanged();
    }
}
```

5. **Save immediately after every state change** (no pooling)

## Performance & Reliability

### Telemetry Performance
- **Async operations**: File writes never block UI thread
- **Batching**: Metrics collected in memory, exported periodically
- **File rotation**: Prevents unlimited disk growth
- **Graceful degradation**: Continues working if file export fails

### State Persistence Performance
- **Immediate writes**: Prevents data loss, acceptable for small datasets
- **JSON serialization**: Fast and lightweight
- **localStorage limits**: ~5-10MB per domain (sufficient for this use case)
- **Error handling**: Gracefully handles localStorage unavailability

### Cross-Platform Compatibility
- **Directory fallback**: Works even with permission restrictions
- **Path normalization**: Handles Windows/Unix path differences
- **Browser compatibility**: localStorage works in all modern browsers

## Troubleshooting

**Telemetry files not appearing**:
- Check application logs for permission errors
- Verify fallback directory: `{app}/bin/telemetry/`
- Ensure directory is writable by application user

**Local storage not persisting**:
- Verify browser allows localStorage (not in incognito/private mode)
- Check browser console for JavaScript errors
- Try clearing browser cache and localStorage

**Component not updating after async operations**:
- Ensure `await InvokeAsync(StateHasChanged())` is called
- Verify render mode is `InteractiveServer`
- Check for unhandled exceptions in async methods

**Build errors on Linux/macOS**:
- Verify .NET 10 SDK is installed: `dotnet --version`
- Check file path case sensitivity (Windows is case-insensitive, Unix is not)
- Ensure all NuGet packages restored: `dotnet restore`

**Questions showing equal values** (should not happen):
- Check service logic for tie detection
- Verify data integrity in `StateData.cs` or `CountryData.cs`
- Review logs for `InvalidOperationException`

## Known Issues & Limitations

**Current Limitations**:
- No user authentication or multi-user support
- No server-side leaderboard (local only)
- No real-time multiplayer mode
- Telemetry stored locally (no central aggregation)
- Manual deployment process

**Future Enhancements Planned**:
- Server-side leaderboard with SQLite database
- User authentication and profiles
- Real-time leaderboard updates
- Automated deployment pipeline
- More question types and datasets
- Multiplayer competitive mode

## Data Sources

**U.S. States** (`StateData.cs`):
- Area: U.S. Census Bureau
- Population: 2020 U.S. Census
- House Representatives: 118th Congress (2023-2025)

**Countries** (`CountryData.cs`):
- Population, GDP, Life Expectancy: World Bank (2023/2024)
- Literacy Rate: UNESCO (latest available)
- CO2 Emissions: Global Carbon Project (2023)
- HDI: UNDP Human Development Report (2024)

**Data Quality**:
- Some countries have missing data (handled gracefully)
- Data represents estimates and may not be exact
- Regular updates planned as new data becomes available

## Contributing

This project is structured for educational purposes and serves as a reference implementation for:
- **Blazor InteractiveServer** patterns
- **OpenTelemetry** instrumentation best practices
- **Cross-platform .NET** development
- **Browser local storage** persistence strategies
- **Mobile-responsive** web design
- **CI/CD** with GitHub Actions

**Contribution Areas**:
- Additional question types and datasets
- Performance optimizations
- UI/UX improvements
- Documentation enhancements
- Test coverage

## License

GNU AFFERO GENERAL PUBLIC LICENSE Version 3

See LICENSE file for full text.

## Repository & Support

**GitHub**: https://github.com/collabskus/HelloAspDotnetTen.git
**Live Demo**: http://open.runasp.net
**Issues**: Report bugs and feature requests via GitHub Issues

## Acknowledgments

This project contains code generated by Large Language Models (Claude and Gemini). All code is experimental and educational in nature. The project demonstrates modern .NET patterns and best practices for Blazor development with comprehensive observability.

## Technical Highlights for Learning

**Software Engineering Principles**:
- ✅ Separation of concerns (Services, Models, Components)
- ✅ Dependency injection throughout
- ✅ Comprehensive error handling
- ✅ Defensive programming (tie detection, data validation)
- ✅ Cross-platform compatibility

**Observability Best Practices**:
- ✅ Distributed tracing with hierarchical spans
- ✅ Rich metrics (counters, histograms, gauges)
- ✅ Structured logging with context
- ✅ Persistent telemetry export
- ✅ Performance-optimized instrumentation

**Data Persistence Patterns**:
- ✅ Immediate consistency (no pooling)
- ✅ Separation of session vs. lifetime data
- ✅ Graceful degradation
- ✅ Complete state restoration
- ✅ Namespace collision prevention

**Modern Web Development**:
- ✅ Mobile-first responsive design
- ✅ Progressive enhancement
- ✅ Accessibility considerations
- ✅ Cross-browser compatibility
- ✅ Performance optimization

---

*Built with .NET 10, Blazor InteractiveServer, and comprehensive OpenTelemetry instrumentation. Deployed to MonsterASP. Maintained by Kushal.*




