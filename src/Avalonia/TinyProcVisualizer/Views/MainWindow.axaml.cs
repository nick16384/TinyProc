using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaHex.Document;
using System.IO;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.GlobalData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";

        ReloadExecutableBinaryFile(null, null);
        SourceBinaryHexEditor.HexView.BytesPerLine = 8;
        //MainHexEditor.HexView.ColumnPadding = 20.0d;
        //MainHexEditor.HexView.FontSize = 22;
        //MainHexEditor.HexView.
        WorkingMemoryHexEditor.HexView.BytesPerLine = 8;
    }

    private void Button_InitCPU_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
        Button_InitCPU.IsEnabled = false;
        Button_ReloadExecutable.IsEnabled = true;
    }

    private MemoryBinaryDocument _currentBinFile;

    private void ReloadExecutableBinaryFile(object? sender, RoutedEventArgs e)
    {
        string binFilePath = "../../../Test Programs/ASMv2/Alphabet.lltp32.bin";
        Console.WriteLine($"Reloading binary file at \"{binFilePath}\"");
        SourceBinaryHexEditor.Document = new MemoryBinaryDocument(File.ReadAllBytes(binFilePath));
    }
}