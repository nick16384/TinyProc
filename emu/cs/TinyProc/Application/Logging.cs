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
    private static void PrintMessageToPipe(string message, Pipe pipe, ConsoleColor color)
    {
        SetConsoleForegroundColor(color);
        switch (pipe)
        {
            case Pipe.STDOUT:
                Console.Write(message);
                break;
            case Pipe.STDERR:
                Console.Error.Write(message);
                Console.ResetColor();
                break;
        }
        ResetConsoleForegroundColor();
    }
    private const ConsoleColor CONSOLE_FG_COLOR_DEBUG = ConsoleColor.White;
    private const ConsoleColor CONSOLE_FG_COLOR_INFO = ConsoleColor.Green;
    private const ConsoleColor CONSOLE_FG_COLOR_WARN = ConsoleColor.Yellow;
    private const ConsoleColor CONSOLE_FG_COLOR_ERROR = ConsoleColor.Red;
    private static readonly ConsoleColor defaultConsoleColor = Console.ForegroundColor;
    private static void SetConsoleForegroundColor(ConsoleColor color) => Console.ForegroundColor = color;
    private static void ResetConsoleForegroundColor() => Console.ForegroundColor = defaultConsoleColor;

    /// <summary>
    /// Counts the time that has elapsed since the process has started.
    /// </summary>
    private static readonly Stopwatch processTimer = Stopwatch.StartNew();

    private static double MILLIS_SINCE_APPLICATION_START
    {
        get => processTimer.Elapsed.TotalSeconds * 1000.0;
    }
    public static bool SuppressDebugMessages { get; set; } = true;
    public static bool SuppressInfoMessages { get; set; } = false;
    public static bool SuppressWarningMessages { get; set; } = false;
    public static bool SuppressErrorMessages { get; set; } = false;

    // Difference between "print" and "log" functions:
    // Print: Writes the specified string directly to stdout / stderr.
    // Log: Concatenates log level and timestamp to the message and adds a line break.

    public static void PrintDebug(string message)
    {
        if (SuppressDebugMessages)
            return;
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogDebugWithoutNewline(string message)
    {
        if (SuppressDebugMessages)
            return;
        PrintMessageToPipe($"[Debug, {MILLIS_SINCE_APPLICATION_START:0000000.000}] ", Pipe.STDOUT, CONSOLE_FG_COLOR_DEBUG);
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogDebug(string message) => LogDebugWithoutNewline(message + "\n");

    public static void PrintInfo(string message)
    {
        if (SuppressInfoMessages)
            return;
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogInfoWithoutNewline(string message)
    {
        if (SuppressInfoMessages)
            return;
        PrintMessageToPipe($"[Info,  {MILLIS_SINCE_APPLICATION_START:0000000.000}] ", Pipe.STDOUT, CONSOLE_FG_COLOR_INFO);
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogInfo(string message) => LogInfoWithoutNewline(message + "\n");

    public static void PrintWarn(string message)
    {
        if (SuppressWarningMessages)
            return;
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogWarnWithoutNewline(string message)
    {
        if (SuppressWarningMessages)
            return;
        PrintMessageToPipe($"[Warn,  {MILLIS_SINCE_APPLICATION_START:0000000.000}] ", Pipe.STDERR, CONSOLE_FG_COLOR_WARN);
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogWarn(string message) => LogWarnWithoutNewline(message + "\n");

    public static void PrintError(string message)
    {
        if (SuppressErrorMessages)
            return;
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogErrorWithoutNewline(string message)
    {
        if (SuppressErrorMessages)
            return;
        PrintMessageToPipe($"[Error, {MILLIS_SINCE_APPLICATION_START:0000000.000}] ", Pipe.STDERR, CONSOLE_FG_COLOR_ERROR);
        PrintMessageToPipe(message, Pipe.STDOUT, defaultConsoleColor);
    }
    public static void LogError(string message) => LogErrorWithoutNewline(message + "\n");

    public static void NewlineDebug()
    {
        if (SuppressDebugMessages)
            return;
        PrintMessageToPipe("\n", Pipe.STDOUT, defaultConsoleColor);
    }
    public static void NewlineInfo()
    {
        if (SuppressInfoMessages)
            return;
        PrintMessageToPipe("\n", Pipe.STDOUT, defaultConsoleColor);
    }
    public static void NewlineWarning()
    {
        if (SuppressWarningMessages)
            return;
        PrintMessageToPipe("\n", Pipe.STDERR, defaultConsoleColor);
    }
    public static void NewlineError()
    {
        if (SuppressErrorMessages)
            return;
        PrintMessageToPipe("\n", Pipe.STDERR, defaultConsoleColor);
    }
}