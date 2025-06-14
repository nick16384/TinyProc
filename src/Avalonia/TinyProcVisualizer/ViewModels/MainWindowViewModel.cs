using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TinyProcVisualizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public static string BorderColorDefault { get => "#0000aa"; }

    public static int RegisterTextWidth { get => 80; }
    public static string BorderColorRegisterPC { get => "#00aaaa"; }
    public static string BorderColorRegisterMAR { get => "#aa00aa"; }
    public static string BorderColorRegisterMDR { get => "#aaaa00"; }
    public static string BorderColorRegisterSR { get => "#dd5500"; }
    public static string BorderColorRegisterGPR { get => "#333333"; }

    public static int HexEditorHeight { get => 450; }
    public static int HexEditorWidth { get => 365; }

    public static double GlobalFontSize { get => 12.0; }
    public static double HexEditorFontSize { get => 12.0; }
    public static double RegisterFontSize { get => 15.0; }

    public static string ToolTip_Register_Fallback { get => "None"; }
    public static string ToolTip_PC_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_PCValue); }
    public static string ToolTip_PC_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_PCValue:D10}"; }
    public static string ToolTip_MAR_ASCII { get => ToolTip_Register_Fallback; }
    public static string ToolTip_MAR_Decimal { get => ToolTip_Register_Fallback; }
    public static string ToolTip_MDR_ASCII { get => ToolTip_Register_Fallback; }
    public static string ToolTip_MDR_Decimal { get => ToolTip_Register_Fallback; }
    public static string ToolTip_SR_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_SRValue); }
    public static string ToolTip_SR_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_SRValue:D10}"; }
    public static string ToolTip_GP1_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP1Value); }
    public static string ToolTip_GP1_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP1Value:D10}"; }
    public static string ToolTip_GP2_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP2Value); }
    public static string ToolTip_GP2_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP2Value:D10}"; }
    public static string ToolTip_GP3_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP3Value); }
    public static string ToolTip_GP3_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP3Value:D10}"; }
    public static string ToolTip_GP4_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP4Value); }
    public static string ToolTip_GP4_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP4Value:D10}"; }
    public static string ToolTip_GP5_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP5Value); }
    public static string ToolTip_GP5_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP5Value:D10}"; }
    public static string ToolTip_GP6_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP6Value); }
    public static string ToolTip_GP6_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP6Value:D10}"; }
    public static string ToolTip_GP7_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP7Value); }
    public static string ToolTip_GP7_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP7Value:D10}"; }
    public static string ToolTip_GP8_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP8Value); }
    public static string ToolTip_GP8_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP8Value:D10}"; }

    private static string ConvertWordToASCII(uint word)
    {
        char c1 = (char)((word & 0xFF000000) >> 24);
        char c2 = (char)((word & 0x00FF0000) >> 16);
        char c3 = (char)((word & 0x0000FF00) >> 8);
        char c4 = (char)((word & 0x000000FF) >> 0);
        // Replace non-printable chars with dots
        if (c1 < 0x20 || c1 > 0x7E) c1 = '.';
        if (c2 < 0x20 || c2 > 0x7E) c2 = '.';
        if (c3 < 0x20 || c3 > 0x7E) c3 = '.';
        if (c4 < 0x20 || c4 > 0x7E) c4 = '.';
        return new string([c1, c2, c3, c4]);
    }

    #region Auto-updated properties

    private long _memoryUsageMebibytes = 0;
    public string MemoryUsage { get => $"RAM Usage: {_memoryUsageMebibytes}MiB"; }

    public MainWindowViewModel() : base()
    {
        _memoryUsageUpdateTimer = new Timer(1000);
        _memoryUsageUpdateTimer.Elapsed += OnMemoryUsageUpdateTimerElapsed;
        _memoryUsageUpdateTimer.Start();
    }

    private Timer _memoryUsageUpdateTimer;

    private void OnMemoryUsageUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _memoryUsageMebibytes = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
        OnPropertyChanged(nameof(MemoryUsage));
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion Auto-updated properties
}
