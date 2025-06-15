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
using System.Diagnostics;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Reflection.Metadata.Ecma335;

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
    // Forces all hex editors to show updated documents, if their length has changed externally.
    // Normal modifications are already handled by the RT updater inside the documents.
    private void ReloadHexEditorDocuments()
    {
        ChangeHexEditorWithNewComboBoxSelection(ComboBox_HexEditor1Selector, HexEditor1);
        ChangeHexEditorWithNewComboBoxSelection(ComboBox_HexEditor2Selector, HexEditor2);
    }

    private async void Button_InitCPU_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
        string? binaryExecutableFilePath = TextBox_BinaryExecutableFilePath.Text;
        if (binaryExecutableFilePath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Unable to initialize CPU",
                "Cannot initialize CPU: Missing binary file.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.ExecutionContainer.Initialize(
            new TinyProc.Application.ExecutableWrapper(binaryExecutableFilePath));

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
        Button_CPURunUntil_MemEqValue.IsEnabled = true;
        Button_CPUFFUntil_MemEqValue.IsEnabled = true;
        Button_CPURunForNCycles.IsEnabled = true;
        Button_CPUFFForNCycles.IsEnabled = true;
        Button_CPURunUntil_RegEqValue.IsEnabled = true;
        Button_CPUFFUntil_RegEqValue.IsEnabled = true;
        // Other
        Button_CPUStop.IsEnabled = true;
        ReloadHexEditorDocuments();
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

    private async void Button_CompileSourceAssemblerFile_OnClick(object? sender, RoutedEventArgs e)
    {
        // Read assembly source file contents
        string? sourceFilePath = TextBox_AssemblySourceFilePath.Text;
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
                $"Runtime compilation error. Message: {ex.Message}\nStacktrace:\n{ex.StackTrace}",
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
        TextBox_BinaryExecutableFilePath.Text = outputBinaryFilePath;
        ReloadHexEditorDocuments();
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

    #region CPU cycle controls

    // Update the TextBox, that shows the GUIs processing overhead time for a cycle
    private void UpdateGUIOverheadTimeTextBox(TimeSpan overheadTime)
    {
        int overheadTimeMicrosecondsTotal = overheadTime.Milliseconds * 1000 + overheadTime.Microseconds;
        Dispatcher.UIThread.Invoke(() =>
        {
            TextBox_CycleTimeGUIOverhead.Text = $"+{overheadTimeMicrosecondsTotal:N0}us";
        });
    }

    private void Button_CPUStepSingleCycle_OnClick(object? sender, RoutedEventArgs e)
    {
        ulong startCycles = TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle;
        RunCPUUntil(() => TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle - startCycles >= 1, true);
    }

    private volatile bool _haltCPUClock = false;
    private void Button_CPURunIndefinitely_OnClick(object? sender, RoutedEventArgs e)
    {
        _haltCPUClock = false;
        RunCPUUntil(() => _haltCPUClock, true);
    }

    private void Button_CPUFastForwardIndefinitely_OnClick(object? sender, RoutedEventArgs e)
    {
        _haltCPUClock = false;
        RunCPUUntil(() => _haltCPUClock);
    }

    private void Button_CPUStop_OnClick(object? sender, RoutedEventArgs e)
        => _haltCPUClock = true;

    private void Button_CPURunUntil_MemEqValue_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntilMemoryAddressHasValue(true);
    private void Button_CPUFFUntil_MemEqValue_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntilMemoryAddressHasValue(false);
    private async void RunCPUUntilMemoryAddressHasValue(bool updateGUIInRealtime)
    {
        uint memAddress;
        uint memValueRequired;
        try
        {
            // TODO: Implement autoconversion from string literals to binary
            memAddress = ConvertStringToUInt(TextBox_CPURunUntil_MemEqValue_Address.Text);
            memValueRequired = ConvertStringToUInt(TextBox_CPURunUntil_MemEqValue_Value.Text);
        }
        catch (FormatException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Unable to parse memory address and/or memory value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (NullReferenceException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Empty memory address and/or value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        _haltCPUClock = false;
        RunCPUUntil(() =>
            TinyProc.Application.ExecutionContainer.INSTANCE0.ReadRAMDirect(memAddress) == memValueRequired,
            updateGUIInRealtime);
    }
    private static uint ConvertStringToUInt(string numStr)
    {
        // Base 2
        if (numStr.StartsWith("0b"))
            return Convert.ToUInt32(numStr, 2);
        // Base 16
        else if (numStr.StartsWith("0x"))
            return Convert.ToUInt32(numStr, 16);
        // Base 10 or unknown
        else
            return Convert.ToUInt32(numStr);
    }

    private void Button_CPURunForNCycles_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUForNCycles(true);
    private void Button_CPUFFForNCycles_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUForNCycles(false);
    private async void RunCPUForNCycles(bool updateGUIInRealtime)
    {
        ulong startCycleCount = TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle;
        ulong cyclesToRun;
        try
        {
            cyclesToRun = ConvertStringToUInt(TextBox_CPURunForNCycles_CycleCount.Text);
        }
        catch (FormatException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Unable to parse number of cycles to run for",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (NullReferenceException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Empty number of cycles to run for",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        _haltCPUClock = false;
        RunCPUUntil(() =>
            TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle - startCycleCount >= cyclesToRun,
            updateGUIInRealtime);
    }

    private void Button_CPURunUntil_RegEqValue_OnClick(object? sender, RoutedEventArgs e)
        => throw new NotImplementedException();
    private void Button_CPUFFUntil_RegEqValue_OnClick(object? sender, RoutedEventArgs e)
        => throw new NotImplementedException();

    private volatile bool _isCPURunning = false;
    private async void RunCPUUntil(Func<bool> haltCondition, bool updateGUIInRealtime = false)
    {
        if (_isCPURunning)
        {
            Console.Error.WriteLine("Cannot run CPU clock: Already running.");
            return;
        }
        _isCPURunning = true;
        if (!updateGUIInRealtime)
            TextBox_CycleTimeGUIOverhead.Text = "-";

        Stopwatch cycleStopwatch = new();
        await Task.Run(() =>
        {
            while (!haltCondition() && !_haltCPUClock)
            {
                TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
                if (updateGUIInRealtime)
                {
                    cycleStopwatch.Restart();
                    SyncCPUandGUIData();
                    UpdateGUIOverheadTimeTextBox(cycleStopwatch.Elapsed);
                }
            }
        });
        _isCPURunning = false;
    }

    #endregion CPU cycle controls
    
    #endregion User event handlers
}