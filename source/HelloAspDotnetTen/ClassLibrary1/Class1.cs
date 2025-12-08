using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClassLibrary1;

public class Class1 : IClass1
{
    private static readonly ActivitySource ActivitySource = new("HelloDotnetTen.ClassLibrary1");
    private static readonly Meter Meter = new("HelloDotnetTen.ClassLibrary1");
    
    // Metrics
    private static readonly Counter<long> _initializationCounter = Meter.CreateCounter<long>(
        "class1.initializations",
        description: "Number of Class1 instances created");
    
    private static readonly Counter<long> _methodCallCounter = Meter.CreateCounter<long>(
        "class1.method_calls",
        description: "Number of times GetLengthOfInjectedProperty was called");
    
    private static readonly Histogram<int> _propertyLengthHistogram = Meter.CreateHistogram<int>(
        "class1.property_length",
        unit: "characters",
        description: "Distribution of property length values");
    
    private readonly Class1Options _options;
    private readonly ILogger<Class1> _logger;

    public Class1(IOptions<Class1Options> options, ILogger<Class1> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Start a span for initialization
        using var activity = ActivitySource.StartActivity("Class1.Initialize");
        
        if (string.IsNullOrEmpty(_options.InjectedProperty1))
        {
            _logger.LogError("InjectedProperty1 cannot be null or empty");
            activity?.SetStatus(ActivityStatusCode.Error, "InjectedProperty1 validation failed");
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", "ArgumentException");
            throw new ArgumentException("Property cannot be null or empty", nameof(_options.InjectedProperty1));
        }

        // Record successful initialization
        _initializationCounter.Add(1, 
            new KeyValuePair<string, object?>("status", "success"),
            new KeyValuePair<string, object?>("property_length", _options.InjectedProperty1.Length));
        
        activity?.SetTag("property.length", _options.InjectedProperty1.Length);
        activity?.SetTag("initialization.status", "success");
        
        _logger.LogInformation(
            "Class1 initialized successfully with InjectedProperty1: {Property} (Length: {Length})", 
            _options.InjectedProperty1,
            _options.InjectedProperty1.Length);
    }

    public int GetLengthOfInjectedProperty()
    {
        // Start trace span
        using var activity = ActivitySource.StartActivity("Class1.GetLengthOfInjectedProperty");
        
        // Record method call
        _methodCallCounter.Add(1);
        
        _logger.LogDebug("Getting length of InjectedProperty1");
        
        var startTime = Stopwatch.GetTimestamp();
        var length = _options.InjectedProperty1.Length;
        var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
        
        // Record metrics
        _propertyLengthHistogram.Record(length);
        
        // Add trace tags
        activity?.SetTag("property.length", length);
        activity?.SetTag("property.value", _options.InjectedProperty1);
        activity?.SetTag("operation.duration_ms", elapsedMs);
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        // Log with structured data
        _logger.LogInformation(
            "InjectedProperty1 length is {Length} (calculated in {DurationMs}ms)", 
            length, 
            elapsedMs);
        
        return length;
    }
}
