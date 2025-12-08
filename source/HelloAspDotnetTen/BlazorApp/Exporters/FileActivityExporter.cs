using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;

namespace BlazorApp.Exporters;

/// <summary>
/// Custom file exporter for OpenTelemetry traces (Activities).
/// Writes traces to JSON files with run-based separation, daily rotation, and size limits.
/// Each application run creates new files (never appends to existing files from previous runs).
/// </summary>
public class FileActivityExporter : BaseExporter<Activity>
{
    private readonly FileExporterOptions _options;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _currentFileDate;
    private long _currentFileSize;
    private int _currentFileNumber;
    private bool _isFirstRecord = true;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileActivityExporter(FileExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        EnsureDirectoryExists();
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();
        
        try
        {
            lock (_lock)
            {
                EnsureWriter();
                
                foreach (var activity in batch)
                {
                    var record = SerializeActivity(activity);
                    var json = JsonSerializer.Serialize(record, _jsonOptions);
                    
                    var bytesToWrite = Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length + 1;
                    
                    if (ShouldRotate(bytesToWrite))
                    {
                        RotateFile();
                    }
                    
                    if (!_isFirstRecord)
                    {
                        _writer!.WriteLine(",");
                    }
                    else
                    {
                        _isFirstRecord = false;
                    }
                    
                    _writer!.Write(json);
                    _currentFileSize += bytesToWrite;
                }
                
                _writer!.Flush();
            }
            
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[FileActivityExporter] Export failed: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    private bool ShouldRotate(long bytesToWrite)
    {
        if (_currentFileSize + bytesToWrite > _options.MaxFileSizeBytes)
            return true;
        
        if (_currentFileDate != DateTime.UtcNow.Date)
            return true;
        
        return false;
    }

    private object SerializeActivity(Activity activity)
    {
        return new
        {
            Timestamp = activity.StartTimeUtc.ToString("O"),
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = activity.ParentSpanId.ToString(),
            OperationName = activity.OperationName,
            DisplayName = activity.DisplayName,
            Kind = activity.Kind.ToString(),
            Status = new
            {
                Code = activity.Status.ToString(),
                Description = activity.StatusDescription
            },
            StartTime = activity.StartTimeUtc.ToString("O"),
            EndTime = (activity.StartTimeUtc + activity.Duration).ToString("O"),
            DurationMs = activity.Duration.TotalMilliseconds,
            Tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value?.ToString()),
            Events = activity.Events.Select(e => new
            {
                Name = e.Name,
                Timestamp = e.Timestamp.ToString("O"),
                Attributes = e.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString())
            }).ToArray(),
            Links = activity.Links.Select(l => new
            {
                TraceId = l.Context.TraceId.ToString(),
                SpanId = l.Context.SpanId.ToString()
            }).ToArray(),
            Resource = ParentProvider?.GetResource()?.Attributes
                .ToDictionary(a => a.Key, a => a.Value?.ToString())
        };
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_options.Directory))
        {
            Directory.CreateDirectory(_options.Directory);
        }
    }

    private void EnsureWriter()
    {
        var today = DateTime.UtcNow.Date;
        
        if (_writer == null)
        {
            OpenNewFile(today);
        }
        else if (_currentFileDate != today)
        {
            RotateFile();
        }
    }

    private void OpenNewFile(DateTime date)
    {
        _currentFileDate = date;
        _currentFileNumber = 0;
        _currentFilePath = GetFilePath(date, _currentFileNumber);
        
        _writer = new StreamWriter(_currentFilePath, append: false, Encoding.UTF8);
        _currentFileSize = 0;
        _isFirstRecord = true;
        
        _writer.WriteLine("[");
        _currentFileSize += 2;
    }

    private void RotateFile()
    {
        CloseWriter();
        
        _currentFileNumber++;
        _currentFileDate = DateTime.UtcNow.Date;
        _currentFilePath = GetFilePath(_currentFileDate, _currentFileNumber);
        
        _writer = new StreamWriter(_currentFilePath, append: false, Encoding.UTF8);
        _currentFileSize = 0;
        _isFirstRecord = true;
        
        _writer.WriteLine("[");
        _currentFileSize += 2;
    }

    private string GetFilePath(DateTime date, int fileNumber)
    {
        var fileName = fileNumber == 0
            ? $"traces_{_options.RunId}.json"
            : $"traces_{_options.RunId}_{fileNumber:D3}.json";
        return Path.Combine(_options.Directory, fileName);
    }

    private void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.WriteLine();
            _writer.WriteLine("]");
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        lock (_lock)
        {
            CloseWriter();
        }
        return true;
    }
}
