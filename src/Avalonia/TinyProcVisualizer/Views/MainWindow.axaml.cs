using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHex;
using Avalonia.Controls.ApplicationLifetimes;
using TinyProcVisualizer.ViewModels;
using AvaloniaHex.Rendering;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.VersionData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";
        TinyProc.Application.Logging.SuppressDebugMessages = true;
        TinyProc.Application.Logging.SuppressInfoMessages = true;

        HexEditor1.HexView.BytesPerLine = 8;
        HexEditor2.HexView.BytesPerLine = 8;
        ComboBox_HexEditor1Selector.SelectedItem = MainWindowViewModel.HexViewSourceSelectionValues[0];
        ComboBox_HexEditor2Selector.SelectedItem = MainWindowViewModel.HexViewSourceSelectionValues[1];
        HexEditor1.Document = HexEditorDocumentBinaryExecutableFile;
        HexEditor2.Document = HexEditorDocumentRAM;

        HexEditor1.HexView.LineTransformers.Add(HexEditorPCHighlighter);
        HexEditor2.HexView.LineTransformers.Add(HexEditorPCHighlighter);
        HexEditor1.HexView.LineTransformers.Add(HexEditorMARHighlighter);
        HexEditor2.HexView.LineTransformers.Add(HexEditorMARHighlighter);

        // TODO: Launch updater thread as daemon
        InitCPUGUIDataSyncThread();
        CPUGUIDataSyncThread.Start();
    }

    // Helper method: Updates a property, if its getter implements some sort of auto-update.
    private static void ForceGetterUpdate(object obj) { object unused = obj; }

    public void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Console.WriteLine("App exit called.");
        _haltCPUGUIDataSyncThread = true;
        _haltCPUClock = true; // Not necessary
    }

    #region Hex Editors

    private static readonly RealTimeFixedSizeBinaryDocument EMPTY_RT_DOCUMENT = new([], null);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_BINARY = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_RAM = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_CON = TimeSpan.FromMilliseconds(100);

    private readonly RangesHighlighter HexEditorPCHighlighter = new()
    {
        Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255), 1.0)
    };
    private readonly RangesHighlighter HexEditorMARHighlighter = new()
    {
        Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 255), 1.0)
    };
    // TODO: Make arbitrary register contents be interpretable as addresses and highlight them.

    private RealTimeFixedSizeBinaryDocument _HexEditorDocumentBinaryExecutableFile = EMPTY_RT_DOCUMENT;
    private RealTimeFixedSizeBinaryDocument HexEditorDocumentBinaryExecutableFile
    {
        get
        {
            // Update contents from file and return
            string? binFilePath = _binaryExecutableFilePath;
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
                ReadOnlySpan<byte> ramData = TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes;
                if ((ulong)ramData.Length != _HexEditorDocumentRAM.Length)
                    _HexEditorDocumentRAM = new RealTimeFixedSizeBinaryDocument(ramData, UPDATE_INTERVAL_DOC_RAM);
                _HexEditorDocumentRAM.WriteNewDataToLiveBuffer(ramData);
            }
            return _HexEditorDocumentRAM;
        }
    }
    private RealTimeFixedSizeBinaryDocument HexEditorDocumentCON
    {
        get => new([3, 4, 5], TimeSpan.FromMilliseconds(1000));
    }

    #endregion Hex Editors

    #region User event handlers

    private void ComboBox_HexEditor1Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor1);
    private void ComboBox_HexEditor2Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ChangeHexEditorWithNewComboBoxSelection(sender as ComboBox, HexEditor2);
    private const string COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE = "Binary executable";
    private const string COMBOBOX_HEXEDITOR_SOURCE_WORKINGMEMORY = "Working memory (RAM)";
    private const string COMBOBOX_HEXEDITOR_SOURCE_CONSOLEMEMORY = "Console memory (CON)";
    private const string COMBOBOX_HEXEDITOR_SOURCE_VIRTUALMEMORY = "Virtual memory";
    private async void ChangeHexEditorWithNewComboBoxSelection(ComboBox? comboBox, HexEditor editor)
    {
        if (editor == null) return;
        editor.HexView.LineTransformers.Clear();
        switch ((comboBox?.SelectedItem as ComboBoxItem)?.Content)
        {
            case COMBOBOX_HEXEDITOR_SOURCE_BINARYEXECUTABLE:
                // FIXME: Make editor still scrollable while preventing edits.
                editor.Document = HexEditorDocumentBinaryExecutableFile;
                break;
            case COMBOBOX_HEXEDITOR_SOURCE_WORKINGMEMORY:
                editor.Document = await Task.Run(() => HexEditorDocumentRAM);
                editor.HexView.LineTransformers.Add(HexEditorPCHighlighter);
                editor.HexView.LineTransformers.Add(HexEditorMARHighlighter);
                break;
            case COMBOBOX_HEXEDITOR_SOURCE_CONSOLEMEMORY:
                editor.Document = await Task.Run(() => HexEditorDocumentCON);
                break;
            default:
                // Default actions (e.g. reenable edits (see FIXME above))
                return;
        }
    }
    // Forces all hex editors to show updated documents, if their document length has changed externally.
    // Normal modifications are already handled by the RT updater inside the documents.
    private void ReloadHexEditorDocuments()
    {
        ChangeHexEditorWithNewComboBoxSelection(ComboBox_HexEditor1Selector, HexEditor1);
        ChangeHexEditorWithNewComboBoxSelection(ComboBox_HexEditor2Selector, HexEditor2);
    }

    private async void Button_InitCPU_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
        string? binaryExecutableFilePath = _binaryExecutableFilePath;
        if (binaryExecutableFilePath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Unable to initialize CPU",
                "Cannot initialize CPU: Missing binary file.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        try
        {
            TinyProc.Application.ExecutionContainer.Initialize(
                new TinyProc.Application.ExecutableWrapper(binaryExecutableFilePath));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Unable to initialize CPU",
                $"Cannot initialize CPU: Error during initialization phase.\nMessage: {ex.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        _HexEditorDocumentRAM =
            await Task.Run(() =>
            new RealTimeFixedSizeBinaryDocument(new byte[TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes.Length],
            UPDATE_INTERVAL_DOC_RAM));
        
        // Simple CPU stepping
        Button_InitCPU.IsEnabled = false;
        Button_CPUStepSingleCycle.IsEnabled = true;
        Button_CPURunIndefinitely.IsEnabled = true;
        Button_CPUFastForwardIndefinitely.IsEnabled = true;
        // Conditional CPU stepping
        StackPanel_AdvancedCycleControl.IsEnabled = true;
        // Other
        Button_CPUStop.IsEnabled = true;
        ReloadHexEditorDocuments();
    }

    private string? _assemblySourceFilePath;
    private string? _binaryExecutableFilePath;

    private async void Menu_File_AssemblySourceFileSelectAndCompile_OnClick(object? sender, RoutedEventArgs e)
    {
        // Load assembly file
        var files = await OpenSingleFileSelectionDialog("Open Assembly Source Code file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Assembly source file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected assembly source file: " + files[0].Name);
        _assemblySourceFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);

        // Compile loaded assembly file
        // Read assembly source file contents
        string? sourceFilePath = _assemblySourceFilePath;
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Compile error",
                "No assembly file selection found. Aborting compilation.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        string sourceFileText = File.ReadAllText(sourceFilePath);

        // Compile source file to binary and save to binary file
        uint[] compiledBinary;
        try
        {
            // Using async await, since the compilation process might take a long time
            compiledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceFileText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Compile error",
                $"Runtime compilation error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;

            // TODO: Show SR contents as flags (text (e.g. ZR, NO, etc.), which is either highlighted or not)
            // Add autofollow PC / MAR checkboxes
            // CPU diagram with arrows (CU, ALU, GPRs, flags, busses->Add bus data and source/target to debug port)
            // See other TODOs esp. in WindowMain.axaml
        }
        TinyProc.Application.ExecutableWrapper programWrapper = new(compiledBinary);
        string outputBinaryFilePath = sourceFilePath + ".bin";
        if (sourceFilePath.EndsWith(".asm"))
            outputBinaryFilePath = sourceFilePath[..^4] + ".bin";
        await Task.Run(() => programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath));

        // Set binary file in GUI
        _binaryExecutableFilePath = outputBinaryFilePath;
        ReloadHexEditorDocuments();
    }
    private async void Menu_File_BinaryExecutableFileSelect_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Open Binary Executable file...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file: " + files[0].Name);
        _binaryExecutableFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        ReloadHexEditorDocuments();
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

    #region Logging

    private void CheckBox_LogDebugMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressDebugMessages = !CheckBox_LogDebugMessages.IsChecked.Value;
    private void CheckBox_LogInfoMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressInfoMessages = !CheckBox_LogInfoMessages.IsChecked.Value;
    private void CheckBox_LogWarningMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressWarningMessages = !CheckBox_LogWarningMessages.IsChecked.Value;
    private void CheckBox_LogErrorMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressErrorMessages = !CheckBox_LogErrorMessages.IsChecked.Value;

    
    #endregion Logging
    
    #endregion User event handlers
}