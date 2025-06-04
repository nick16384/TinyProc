using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Web;
using System.Collections.Generic;
using System.IO;
using AvaloniaHex.Document;
using System.Threading.Tasks;
using AvaloniaHex;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.GlobalData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";

        HexEditor1.HexView.BytesPerLine = 8;
        HexEditor2.HexView.BytesPerLine = 8;
    }

    private void ComboBox_HexEditor1Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor1);
    private void ComboBox_HexEditor2Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor2);
    private const string COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE = "Binary executable";
    private const string COMBOBOX_HEXEDITOR_SOURCE_WORKINGMEMORY = "Working memory (RAM)";
    private const string COMBOBOX_HEXEDITOR_SOURCE_CONSOLEMEMORY = "Console memory (CON)";
    private void ChangeHexEditorWithNewComboBoxSelection(ComboBox? comboBox, HexEditor editor)
    {
        switch ((comboBox?.SelectedItem as ComboBoxItem)?.Content)
        {
            case COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE:
                string? binFilePath = TextBox_BinaryExecutableFilePath?.Text;
                if (string.IsNullOrWhiteSpace(binFilePath))
                {
                    Console.Error.WriteLine("Cannot apply hex editor document: Binary executable file path is null or whitespace.");
                    break;
                }
                editor.IsEnabled = false;
                editor.Document = new MemoryBinaryDocument(File.ReadAllBytes(binFilePath));
                break;

            default:
                if (editor != null) editor.IsEnabled = true;
                return;
        }
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

    private async void Button_OpenAssemblySourceFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Open Assembly Source Code file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Assembly source file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected assembly source file: " + files[0].Name);
        TextBox_AssemblySourceFilePath.Text = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        Button_CompileSourceAssemblerFile.IsEnabled = true;
    }
    private async void Button_OpenBinaryExecutableFilePath_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Open Binary Executable file...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file: " + files[0].Name);
        string binFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        TextBox_BinaryExecutableFilePath.Text = binFilePath;
        HexEditor1.Document = new MemoryBinaryDocument(File.ReadAllBytes(binFilePath));
    }
    private async Task<IReadOnlyList<IStorageFile>> OpenSingleFileSelectionDialog(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        return files;
    }

    private async void Button_CompileSourceAssemblerFile_OnClick(object? sender, RoutedEventArgs e)
    {
        // Read assembly source file contents
        string? sourceFilePath = TextBox_AssemblySourceFilePath.Text;
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.Error.WriteLine("No assembly file selection found. Aborting compilation.");
            return;
        }
        string sourceFileText = File.ReadAllText(sourceFilePath);

        // Compile source file to binary and save to binary file
        // Using async await, since the compilation process might take a long time
        uint[] compiledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceFileText));
        TinyProc.Application.ExecutableWrapper programWrapper = new(compiledBinary);
        string outputBinaryFilePath = sourceFilePath + ".bin";
        if (sourceFilePath.EndsWith(".asm"))
            outputBinaryFilePath = sourceFilePath[..^4] + ".bin";
        programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath);

        // Set binary file in GUI
        TextBox_BinaryExecutableFilePath.Text = outputBinaryFilePath;
        // TODO: Make this more clean (add separate Update() method and real-time changing documents)
        if ((ComboBox_HexEditor1Selector.SelectedItem as ComboBoxItem)?.Content == COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE)
            HexEditor1.Document = new MemoryBinaryDocument(File.ReadAllBytes(outputBinaryFilePath));
        if ((ComboBox_HexEditor2Selector.SelectedItem as ComboBoxItem)?.Content == COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE)
            HexEditor2.Document = new MemoryBinaryDocument(File.ReadAllBytes(outputBinaryFilePath));
        
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
        => TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
}