using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace BlazorApp.Exporters;

/// <summary>
/// Custom file exporter for OpenTelemetry logs.
/// Writes logs to JSON files with run-based separation, daily rotation, and size limits.
/// Each application run creates new files (never appends to existing files from previous runs).
/// </summary>
public class FileLogExporter : BaseExporter<LogRecord>
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

    public FileLogExporter(FileExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        EnsureDirectoryExists();
    }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();
        
        try
        {
            lock (_lock)
            {
                EnsureWriter();
                
                foreach (var logRecord in batch)
                {
                    var record = SerializeLogRecord(logRecord);
                    var json = JsonSerializer.Serialize(record, _jsonOptions);
                    
                    var bytesToWrite = Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length + 1; // +1 for comma
                    
                    // Check if we need to rotate (size limit or day change)
                    if (ShouldRotate(bytesToWrite))
                    {
                        RotateFile();
                    }
                    
                    // Write comma separator if not first record
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
            System.Console.Error.WriteLine($"[FileLogExporter] Export failed: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    private bool ShouldRotate(long bytesToWrite)
    {
        // Rotate if size would exceed limit
        if (_currentFileSize + bytesToWrite > _options.MaxFileSizeBytes)
            return true;
        
        // Rotate if day has changed (for long-running applications)
        if (_currentFileDate != DateTime.UtcNow.Date)
            return true;
        
        return false;
    }

    private object SerializeLogRecord(LogRecord logRecord)
    {
        var attributes = new Dictionary<string, object?>();
        
        if (logRecord.Attributes != null)
        {
            foreach (var attr in logRecord.Attributes)
            {
                attributes[attr.Key] = attr.Value;
            }
        }

        logRecord.ForEachScope(ProcessScope, attributes);

        return new
        {
            Timestamp = logRecord.Timestamp.ToString("O"),
            TraceId = logRecord.TraceId.ToString(),
            SpanId = logRecord.SpanId.ToString(),
            TraceFlags = logRecord.TraceFlags.ToString(),
            CategoryName = logRecord.CategoryName,
            LogLevel = logRecord.LogLevel.ToString(),
            Body = logRecord.Body,
            FormattedMessage = logRecord.FormattedMessage,
            Attributes = attributes,
            EventId = logRecord.EventId.Id != 0 ? 
                new { logRecord.EventId.Id, logRecord.EventId.Name } : null,
            Exception = logRecord.Exception?.ToString(),
            Resource = ParentProvider?.GetResource()?.Attributes
                .ToDictionary(a => a.Key, a => a.Value?.ToString())
        };
    }

    private static readonly Action<LogRecordScope, Dictionary<string, object?>> ProcessScope = 
        (scope, attributes) =>
        {
            foreach (var item in scope)
            {
                attributes[$"scope.{item.Key}"] = item.Value;
            }
        };

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
            // Day changed - rotate to new file
            RotateFile();
        }
    }

    private void OpenNewFile(DateTime date)
    {
        _currentFileDate = date;
        _currentFileNumber = 0;
        _currentFilePath = GetFilePath(date, _currentFileNumber);
        
        // For this run, always create a new file (never append)
        _writer = new StreamWriter(_currentFilePath, append: false, Encoding.UTF8);
        _currentFileSize = 0;
        _isFirstRecord = true;
        
        // Start JSON array
        _writer.WriteLine("[");
        _currentFileSize += 2; // "[\n"
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
        
        // Start JSON array
        _writer.WriteLine("[");
        _currentFileSize += 2;
    }

    private string GetFilePath(DateTime date, int fileNumber)
    {
        // Format: logs_YYYYMMDD_HHmmss_NNN.json
        // The RunId contains the start time, fileNumber handles rotation within a run
        var fileName = fileNumber == 0
            ? $"logs_{_options.RunId}.json"
            : $"logs_{_options.RunId}_{fileNumber:D3}.json";
        return Path.Combine(_options.Directory, fileName);
    }

    private void CloseWriter()
    {
        if (_writer != null)
        {
            // Close JSON array
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
