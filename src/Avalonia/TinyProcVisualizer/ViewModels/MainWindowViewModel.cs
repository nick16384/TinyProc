using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Avalonia.Controls;
using Timer = System.Timers.Timer;

namespace TinyProcVisualizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public static string BorderColorDefault { get => "#0000aa"; }

    public static int RegisterTextWidth { get => 80; }
    public static string BorderColorRegisterPC { get => "#00aaaa"; }
    public static string BorderColorRegisterIRA { get => "#aa00aa"; }
    public static string BorderColorRegisterIRB { get => BorderColorRegisterIRA; }
    public static string BorderColorRegisterSR { get => "#dd5500"; }
    public static string BorderColorRegisterGPR { get => "#333333"; }
    public static string BorderColorRegisterMAR { get => "#993333"; }
    public static string BorderColorRegisterMDR { get => "#bb3333"; }

    // TODO: Move this scaling code into a separate class
    #region Element scaling
    public static int WindowWidth { get; } = 800;
    public static int WindowHeight { get; } = 600;

    public static double ToolbarScaleWidth { get; } = 1.0;
    public static double ToolbarScaleHeight { get; } = 0.1;

    public static double Grid_AdvancedCPUCycling_ScaleWidth { get; } = 0.3;
    public static double Grid_AdvancedCPUCycling_Column0_ScaleWidth { get; } = 0.2;

    // TODO: Implement this style of relative scaling for all window elements
    // This is because of varying resolutions and I want the app to be fullscreen
    public static double ToolbarWidth { get => WindowWidth * ToolbarScaleWidth; }
    public static double ToolbarHeight { get => WindowWidth * ToolbarScaleHeight; }
    public static double Grid_AdvancedCPUCycling_Width { get => WindowWidth * Grid_AdvancedCPUCycling_ScaleWidth; }
    public static double Grid_AdvancedCPUCycling_Column0_Width { get => Grid_AdvancedCPUCycling_Column0_Width * Grid_AdvancedCPUCycling_Column0_ScaleWidth; }

    #endregion Element scaling

    public static int HexEditorHeight { get => 450; }
    public static int HexEditorWidth { get => 365; }

    public static double GlobalFontSize { get => 12.0; }
    public static double HexEditorFontSize { get => 12.0; }
    public static double RegisterFontSize { get => 15.0; }

    public static string ToolTip_Register_Fallback { get => "None"; }

    // Special-value registers
    public static string ToolTip_PC_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue); }
    public static string ToolTip_PC_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue:D10}"; }
    public static string ToolTip_IRA_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRAValue); }
    public static string ToolTip_IRA_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRAValue:D10}"; }
    public static string ToolTip_IRB_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRBValue); }
    public static string ToolTip_IRB_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRBValue:D10}"; }
    public static string ToolTip_SR_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.SRValue); }
    public static string ToolTip_SR_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.SRValue:D10}"; }
    public static string ToolTip_MAR_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue); }
    public static string ToolTip_MAR_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue:D10}"; }
    public static string ToolTip_MDR_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MDRValue); }
    public static string ToolTip_MDR_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MDRValue:D10}"; }

    // General-purpose registers
    public static string ToolTip_GP1_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP1Value); }
    public static string ToolTip_GP1_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP1Value:D10}"; }
    public static string ToolTip_GP2_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP2Value); }
    public static string ToolTip_GP2_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP2Value:D10}"; }
    public static string ToolTip_GP3_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP3Value); }
    public static string ToolTip_GP3_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP3Value:D10}"; }
    public static string ToolTip_GP4_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP4Value); }
    public static string ToolTip_GP4_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP4Value:D10}"; }
    public static string ToolTip_GP5_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP5Value); }
    public static string ToolTip_GP5_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP5Value:D10}"; }
    public static string ToolTip_GP6_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP6Value); }
    public static string ToolTip_GP6_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP6Value:D10}"; }
    public static string ToolTip_GP7_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP7Value); }
    public static string ToolTip_GP7_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP7Value:D10}"; }
    public static string ToolTip_GP8_ASCII { get => ConvertWordToASCII(TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP8Value); }
    public static string ToolTip_GP8_Decimal { get => $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP8Value:D10}"; }

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

    public static int Buttons_CycleControl_SizeXY { get => 16; }

    // TODO: Make ComboBoxItems constant and add them to an enum (or equivalent)
    // so that checking the selected element in MainWindow has a single reference here.

    public static List<ComboBoxItem> HexViewSourceSelectionValues { get; } = [
        new() { Content = "Binary executable" },
        new() { Content = "Working memory (RAM)" },
        new() { Content = "Console memory (CON)" },
        new() { Content = "Virtual memory" }
    ];

    public static List<ComboBoxItem> RegisterSelectionValues { get; } = [
        new() { Content = "GPR 1" },
        new() { Content = "GPR 2" },
        new() { Content = "GPR 3" },
        new() { Content = "GPR 4" },
        new() { Content = "GPR 5" },
        new() { Content = "GPR 6" },
        new() { Content = "GPR 7" },
        new() { Content = "GPR 8" },
        new() { Content = "PC" },
        new() { Content = "IRA" },
        new() { Content = "IRB" },
        // MAR and MDR change multiple times during a single cycle, so MT would mess things up (probably).
        /*new() { Content = "MAR" },
        new() { Content = "MDR" },*/
        new() { Content = "SR" },
        ];

    #region Auto-updated properties

    private long _memoryUsageMebibytes = 0;
    public string MemoryUsage { get => $"Host RAM Usage: {_memoryUsageMebibytes}MiB"; }

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
