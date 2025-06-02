using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaHex.Document;
using System.IO;
using Avalonia.Platform.Storage;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.GlobalData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";

        SourceBinaryHexEditor.HexView.BytesPerLine = 8;
        //MainHexEditor.HexView.ColumnPadding = 20.0d;
        //MainHexEditor.HexView.FontSize = 22;
        //MainHexEditor.HexView.
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
        Button_ReloadExecutable.IsEnabled = true;
        Button_CPUStepSingleCycle.IsEnabled = true;

        // TODO: Add real-time updating MemoryBinaryDocument for CPU RAM
    }

    private async void Button_OpenAssemblySourceFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Assembly Source Code file...",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            Console.WriteLine("Selected assembly source file: " + files[0].Name);
            /*await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            string assemblySourceFileContent = await streamReader.ReadToEndAsync();*/
            TextBox_AssemblySourceFilePath.Text = files[0].Path.AbsolutePath;
        }
    }
    private async void Button_OpenBinaryExecutableFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Binary Executable file...",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            Console.WriteLine("Selected binary executable file: " + files[0].Name);
            /*await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            string assemblySourceFileContent = await streamReader.ReadToEndAsync();*/
            TextBox_BinaryExecutableFilePath.Text = files[0].Path.AbsolutePath.Replace("%20", " ");
            ReloadExecutableBinaryFile(null, null);
        }
    }

    private void ReloadExecutableBinaryFile(object? sender, RoutedEventArgs e)
    {
        string binFilePath = TextBox_BinaryExecutableFilePath.Text;
        Console.WriteLine($"Reloading binary file at \"{binFilePath}\"");
        SourceBinaryHexEditor.Document = new MemoryBinaryDocument(File.ReadAllBytes(binFilePath));
    }

    private void CPUStepSingleCycle(object? sender, RoutedEventArgs e)
    {
        TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
    }
}