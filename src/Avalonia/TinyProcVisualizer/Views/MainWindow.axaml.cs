using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHex;
using System.Diagnostics;

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

    // Helper method: Updates a property, if its getter implements some sort of auto-update.
    private static void ForceGetterUpdate(object obj) { object unused = obj; }

    #region Hex Editor binary documents

    private static readonly RealTimeFixedSizeBinaryDocument EMPTY_RT_DOCUMENT = new([], TimeSpan.FromMilliseconds(1000));
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_BINARY = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_RAM = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_CON = TimeSpan.FromMilliseconds(100);

    private RealTimeFixedSizeBinaryDocument _HexEditorDocumentBinaryExecutableFile = EMPTY_RT_DOCUMENT;
    private RealTimeFixedSizeBinaryDocument HexEditorDocumentBinaryExecutableFile
    {
        get
        {
            // Update contents from file and return
            string? binFilePath = TextBox_BinaryExecutableFilePath?.Text;
            if (string.IsNullOrWhiteSpace(binFilePath) || !File.Exists(binFilePath))
                Console.Error.WriteLine("Cannot apply hex editor document: Binary executable file path is invalid.");
            else
            {
                _HexEditorDocumentBinaryExecutableFile
                    = new RealTimeFixedSizeBinaryDocument(File.ReadAllBytes(binFilePath), UPDATE_INTERVAL_DOC_BINARY);
            }
            return _HexEditorDocumentBinaryExecutableFile;
        }
    }
    private RealTimeFixedSizeBinaryDocument _HexEditorDocumentRAM = EMPTY_RT_DOCUMENT;
    private RealTimeFixedSizeBinaryDocument HexEditorDocumentRAM
    {
        get
        {
            if (TinyProc.Application.ExecutionContainer.INSTANCE0 == null)
                Console.Error.WriteLine("CPU not initialized yet, cannot read RAM.");
            else
            {
                byte[] ramData = TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes;
                _HexEditorDocumentRAM.WriteNewDataToLiveBuffer(ramData);
            }
            return _HexEditorDocumentRAM;
        }
    }
    private RealTimeFixedSizeBinaryDocument HexEditorDocumentCON
    {
        get => new([3, 4, 5], TimeSpan.FromMilliseconds(1000));
    }

    #endregion Hex Editor binary documents

    private void ComboBox_HexEditor1Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor1);
    private void ComboBox_HexEditor2Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor2);
    private const string COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE = "Binary executable";
    private const string COMBOBOX_HEXEDITOR_SOURCE_WORKINGMEMORY = "Working memory (RAM)";
    private const string COMBOBOX_HEXEDITOR_SOURCE_CONSOLEMEMORY = "Console memory (CON)";
    private async void ChangeHexEditorWithNewComboBoxSelection(ComboBox? comboBox, HexEditor editor)
    {
        if (editor == null) return;
        switch ((comboBox?.SelectedItem as ComboBoxItem)?.Content)
        {
            case COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE:
                // FIXME: Make editor still scrollable while preventing edits.
                editor.IsEnabled = false;
                editor.Document = HexEditorDocumentBinaryExecutableFile;
                break;
            case COMBOBOX_HEXEDITOR_SOURCE_WORKINGMEMORY:
                editor.IsEnabled = true;
                editor.Document = await Task.Run(() => HexEditorDocumentRAM);
                break;
            case COMBOBOX_HEXEDITOR_SOURCE_CONSOLEMEMORY:
                editor.IsEnabled = true;
                editor.Document = await Task.Run(() => HexEditorDocumentCON);
                break;
            default:
                editor.IsEnabled = true;
                return;
        }
    }

    private async void Button_InitCPU_OnClick(object? sender, RoutedEventArgs e)
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

        _HexEditorDocumentRAM =
            await Task.Run(() =>
            new RealTimeFixedSizeBinaryDocument(new byte[TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes.Length],
            UPDATE_INTERVAL_DOC_RAM));

        Button_InitCPU.IsEnabled = false;
        Button_CPUStepSingleCycle.IsEnabled = true;
        Button_CPURunIndefinitely.IsEnabled = true;
        Button_CPUStop.IsEnabled = true;
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
        TextBox_BinaryExecutableFilePath.Text = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        HexEditor1.Document = HexEditorDocumentBinaryExecutableFile;
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
        await Task.Run(() => programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath));

        // Set binary file in GUI
        TextBox_BinaryExecutableFilePath.Text = outputBinaryFilePath;
        await Task.Run(() => ForceGetterUpdate(HexEditorDocumentBinaryExecutableFile));
    }

    private void CheckBox_LogDebugMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressDebugMessages = !CheckBox_LogDebugMessages.IsChecked.Value;
    private void CheckBox_LogInfoMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressInfoMessages = !CheckBox_LogInfoMessages.IsChecked.Value;
    private void CheckBox_LogWarningMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressWarningMessages = !CheckBox_LogWarningMessages.IsChecked.Value;
    private void CheckBox_LogErrorMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressErrorMessages = !CheckBox_LogErrorMessages.IsChecked.Value;

    private async void Button_CPUStepSingleCycle_OnClick(object? sender, RoutedEventArgs e)
    {
        TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
        await Task.Run(() => ForceGetterUpdate(HexEditorDocumentRAM));
        await Task.Run(() => ForceGetterUpdate(HexEditorDocumentCON));
    }

    private volatile bool _haltCPUClock = false;
    private async void Button_CPURunIndefinitely_OnClick(object? sender, RoutedEventArgs e)
    {
        while (!_haltCPUClock)
        {
            TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
            Stopwatch ramToBytesSW = Stopwatch.StartNew();
            await Task.Run(() => ForceGetterUpdate(HexEditorDocumentRAM));
            await Task.Run(() => ForceGetterUpdate(HexEditorDocumentCON));
            Console.WriteLine($"Converting uints to bytes took {ramToBytesSW.ElapsedMilliseconds}ms");
        }
    }

    private void Button_CPUStop_OnClick(object? sender, RoutedEventArgs e)
    {
        _haltCPUClock = true;
    }
}