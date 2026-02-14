using System.Diagnostics;

namespace TinyProc.Application;

public class Logging
{
    // TODO: Make log files attachable
    public enum Pipe
    {
        STDOUT,
        STDERR
    }
    private static void PrintMessageToPipe(string message, Pipe pipe)
    {
        switch (pipe)
        {
            case Pipe.STDOUT:
                Console.Write(message);
                break;
            case Pipe.STDERR:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(message);
                Console.ResetColor();
                break;
        }
    }

    /// <summary>
    /// Counts the time that has elapsed since the process has started.
    /// </summary>
    private static readonly Stopwatch processTimer = Stopwatch.StartNew();

    private static double MILLIS_SINCE_APPLICATION_START
    {
        get => processTimer.Elapsed.Milliseconds + (0.001 * processTimer.Elapsed.Microseconds);
    }
    public static bool SuppressDebugMessages { get; set; } = false;
    public static bool SuppressInfoMessages { get; set; } = false;
    public static bool SuppressWarningMessages { get; set; } = false;
    public static bool SuppressErrorMessages { get; set; } = false;

    // Difference between "print" and "log" functions:
    // Print: Writes the specified string directly to stdout / stderr.
    // Log: Concatenates log level and timestamp to the message and adds a line break.

    public static void PrintDebug(string message)
    {
        if (!SuppressDebugMessages)
            PrintMessageToPipe(message, Pipe.STDOUT);
    }
    public static void LogDebugWithoutNewline(string message)
    {
        if (!SuppressDebugMessages)
            PrintMessageToPipe($"[Debug, {MILLIS_SINCE_APPLICATION_START:0000000.000}] {message}", Pipe.STDOUT);
    }
    public static void LogDebug(string message) => LogDebugWithoutNewline(message + "\n");

    public static void PrintInfo(string message)
    {
        if (!SuppressInfoMessages)
            PrintMessageToPipe(message, Pipe.STDOUT);
    }
    public static void LogInfoWithoutNewline(string message)
    {
        if (!SuppressInfoMessages)
            PrintMessageToPipe($"[Info,  {MILLIS_SINCE_APPLICATION_START:0000000.000}] {message}", Pipe.STDOUT);
    }
    public static void LogInfo(string message) => LogInfoWithoutNewline(message + "\n");

    public static void PrintWarn(string message)
    {
        if (!SuppressWarningMessages)
            PrintMessageToPipe(message, Pipe.STDOUT);
    }
    public static void LogWarnWithoutNewline(string message)
    {
        if (!SuppressWarningMessages)
            PrintMessageToPipe($"[Warn,  {MILLIS_SINCE_APPLICATION_START:0000000.000}] {message}", Pipe.STDERR);
    }
    public static void LogWarn(string message) => LogWarnWithoutNewline(message + "\n");

    public static void PrintError(string message)
    {
        if (!SuppressErrorMessages)
            PrintMessageToPipe(message, Pipe.STDOUT);
    }
    public static void LogErrorWithoutNewline(string message)
    {
        if (!SuppressErrorMessages)
            PrintMessageToPipe($"[Error, {MILLIS_SINCE_APPLICATION_START:0000000.000}] {message}", Pipe.STDERR);
    }
    public static void LogError(string message) => LogErrorWithoutNewline(message + "\n");

    public static void NewlineDebug()
    {
        if (!SuppressDebugMessages)
            PrintMessageToPipe("\n", Pipe.STDOUT);
    }
    public static void NewlineInfo()
    {
        if (!SuppressInfoMessages)
            PrintMessageToPipe("\n", Pipe.STDOUT);
    }
    public static void NewlineWarning()
    {
        if (!SuppressWarningMessages)
            PrintMessageToPipe("\n", Pipe.STDERR);
    }
    public static void NewlineError()
    {
        if (!SuppressErrorMessages)
            PrintMessageToPipe("\n", Pipe.STDERR);
    }
}