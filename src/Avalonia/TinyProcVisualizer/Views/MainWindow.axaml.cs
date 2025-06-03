using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Web;
using System.Collections.Generic;
using System.IO;
using AvaloniaHex.Document;
using AvaloniaHex;
using System.Diagnostics;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.GlobalData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";

        SourceBinaryHexEditor.HexView.BytesPerLine = 8;
        WorkingMemoryHexEditor.HexView.BytesPerLine = 8;
    }

    private void Button_InitCPU_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
        string? binaryExecutableFilePath = TextBox_BinaryExecutableFilePath.Text;
        if (binaryExecutableFilePath == null)
        {
            // TODO: Show error in message dialog
            // See https://github.com/AvaloniaCommunity/MessageBox.Avalonia
            Console.Error.WriteLine("Cannot initialize CPU: Missing binary file.");
            return;
        }
        TinyProc.Application.ExecutionContainer.Initialize(
            new TinyProc.Application.ExecutableWrapper(binaryExecutableFilePath));

        Button_InitCPU.IsEnabled = false;
        Button_CPUStepSingleCycle.IsEnabled = true;

        // TODO: Add real-time updating MemoryBinaryDocument for CPU RAM
    }

    private void Button_OpenAssemblySourceFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = OpenSingleFileSelectionDialog("Open Assembly Source Code file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Assembly source file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected assembly source file: " + files[0].Name);
        TextBox_AssemblySourceFilePath.Text = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        Button_CompileSourceAssemblerFile.IsEnabled = true;
    }
    private void Button_OpenBinaryExecutableFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = OpenSingleFileSelectionDialog("Open Binary Executable file...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file: " + files[0].Name);
        string binFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        TextBox_BinaryExecutableFilePath.Text = binFilePath;
        SourceBinaryHexEditor.Document = new MemoryBinaryDocument(File.ReadAllBytes(binFilePath));
    }
    private IReadOnlyList<IStorageFile> OpenSingleFileSelectionDialog(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        }).Result;
        return files;
    }

    private void Button_CompileSourceAssemblerFile_OnClick(object? sender, RoutedEventArgs e)
    {
        // Read assembly source file contents
        string sourceFilePath = TextBox_AssemblySourceFilePath.Text;
        string sourceFileText = File.ReadAllText(sourceFilePath);

        // Compile source file to binary and save to binary file
        uint[] compiledBinary = TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceFileText);
        TinyProc.Application.ExecutableWrapper programWrapper = new(compiledBinary);
        string outputBinaryFilePath = sourceFilePath + ".bin";
        if (sourceFilePath.EndsWith(".asm"))
            outputBinaryFilePath = sourceFilePath[..^4] + ".bin";
        programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath);

        // Set binary file in GUI
        TextBox_BinaryExecutableFilePath.Text = outputBinaryFilePath;
        SourceBinaryHexEditor.Document = new MemoryBinaryDocument(File.ReadAllBytes(outputBinaryFilePath));
    }

    private void CheckBox_LogDebugMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressDebugMessages = !CheckBox_LogDebugMessages.IsChecked.Value;
    private void CheckBox_LogInfoMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressInfoMessages = !CheckBox_LogInfoMessages.IsChecked.Value;
    private void CheckBox_LogWarningMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressWarningMessages = !CheckBox_LogWarningMessages.IsChecked.Value;
    private void CheckBox_LogErrorMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressErrorMessages = !CheckBox_LogErrorMessages.IsChecked.Value;

    private void CPUStepSingleCycle(object? sender, RoutedEventArgs e)
    {
        TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
    }
}