using Avalonia;
using Avalonia.Logging;

namespace TinyProcVisualizer.Logging;

public class TinyProcSink : ILogSink
{
    public TinyProcSink(LogEventLevel level, params string[] areas) {}
    public bool IsEnabled(LogEventLevel level, string area) => true;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        // TODO: Continue testing on Windows
        // On Linux, this prints a bunch of Avalonia stuff (very verbose, not useful)
        //Console.OpenStandardOutput().BeginWrite([45], 0, 1, null, null);
        //Console.WriteLine(messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        //Console.OpenStandardOutput().BeginWrite([45, 46, 47], 0, 1, null, null);
        //Console.WriteLine(messageTemplate);
    }
}

public static class MyLogExtensions
{
    public static AppBuilder LogToTinyProcSink(this AppBuilder builder,
        LogEventLevel level = LogEventLevel.Warning,
        params string[] areas)
    {
        Logger.Sink = new TinyProcSink(level, areas);
        return builder;
    }
}