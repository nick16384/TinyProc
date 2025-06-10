using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHex;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.Application.VersionData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";

        HexEditor1.HexView.BytesPerLine = 8;
        HexEditor2.HexView.BytesPerLine = 8;
        HexEditor1.Document = HexEditorDocumentBinaryExecutableFile;
        HexEditor2.Document = HexEditorDocumentRAM;

        // TODO: This could maybe be made more beautiful
        // Thread that periodically synchronizes the data from the CPU (ExecutionContainer)
        // with the data shown on screen to the user via the GUI.
        Thread CPUGUIDataSyncThread = new(() =>
        {
            _haltCPUGUIDataSyncThread = false;
            while (!_haltCPUGUIDataSyncThread)
            {
                Thread.Sleep(SYNC_INTERVAL_CPU_GUI);
                SyncCPUandGUIData();
            }
        });
        // TODO: Launch updater thread as daemon
        CPUGUIDataSyncThread.Start();
    }

    #region CPU -> GUI data synchronization

    private static readonly TimeSpan SYNC_INTERVAL_CPU_GUI = TimeSpan.FromMilliseconds(200);
    private volatile bool _haltCPUGUIDataSyncThread = false;

    private void SyncCPUandGUIData()
    {
        // Check if the CPU is already initialized. If not, synchronization is unnecessary.
        if (TinyProc.Application.ExecutionContainer.INSTANCE0 == null)
            return;
        // Sync & update current CPU cycle TextBox
        string currentCPUCycle = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle}";
        var updateTextBox_CurrentCPUCycle = Dispatcher.UIThread.InvokeAsync(() => TextBox_CurrentCPUCycle.Text = currentCPUCycle);

        // Sync and update last CPU cycle time
        string lastCycleTime = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.LastCycleTimeMicroseconds}us";
        var updateTextBox_LastCycleTime = Dispatcher.UIThread.InvokeAsync(() => TextBox_LastCPUCycleTime.Text = lastCycleTime);

        // Sync and update register text blocks
        string gp1Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP1Value:x8}";
        string gp2Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP2Value:x8}";
        string gp3Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP3Value:x8}";
        string gp4Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP4Value:x8}";
        string gp5Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP5Value:x8}";
        string gp6Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP6Value:x8}";
        string gp7Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP7Value:x8}";
        string gp8Value = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP8Value:x8}";
        string pcValue = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_PCValue:x8}";
        string srValue = $"0x{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_SRValue:x8}";
        var updateTextBlock_GP1 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR1.Text = gp1Value);
        var updateTextBlock_GP2 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR2.Text = gp2Value);
        var updateTextBlock_GP3 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR3.Text = gp3Value);
        var updateTextBlock_GP4 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR4.Text = gp4Value);
        var updateTextBlock_GP5 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR5.Text = gp5Value);
        var updateTextBlock_GP6 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR6.Text = gp6Value);
        var updateTextBlock_GP7 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR7.Text = gp7Value);
        var updateTextBlock_GP8 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR8.Text = gp8Value);
        var updateTextBlock_PC = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterPC.Text = pcValue);
        var updateTextBlock_SR = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterSR.Text = srValue);

        // Sync RAM and CON hex view (They update themselves)
        var syncRAM = Task.Run(() => ForceGetterUpdate(HexEditorDocumentRAM));
        var syncCON = Task.Run(() => ForceGetterUpdate(HexEditorDocumentCON));

        // Wait for all update tasks to complete
        // FIXME: Fix "Task cancelled" exception when closing Window.
        // It means, there are still some update tasks running when they clearly should not.
        Task.WaitAll([
            updateTextBox_CurrentCPUCycle.GetTask(),
            updateTextBox_LastCycleTime.GetTask(),
            updateTextBlock_GP1.GetTask(),
            updateTextBlock_GP2.GetTask(),
            updateTextBlock_GP3.GetTask(),
            updateTextBlock_GP4.GetTask(),
            updateTextBlock_GP5.GetTask(),
            updateTextBlock_GP6.GetTask(),
            updateTextBlock_GP7.GetTask(),
            updateTextBlock_GP8.GetTask(),
            updateTextBlock_PC.GetTask(),
            updateTextBlock_SR.GetTask(),
            syncRAM,
            syncCON
        ]);
    }

    #endregion CPU -> GUI data synchronization

    // Helper method: Updates a property, if its getter implements some sort of auto-update.
    private static void ForceGetterUpdate(object obj) { object unused = obj; }

    public void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Console.WriteLine("App exit called.");
        _haltCPUGUIDataSyncThread = true;
        _haltCPUClock = true; // Not necessary
    }

    #region Hex Editor binary documents

    private static readonly RealTimeFixedSizeBinaryDocument EMPTY_RT_DOCUMENT = new([], null);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_BINARY = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_RAM = TimeSpan.FromMilliseconds(250);
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
        ReloadHexEditorDocuments();
        TextBox_CurrentCPUCycle.Text = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle}";
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
        TextBox_CurrentCPUCycle.Text = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle}";
        await Task.Run(() => ForceGetterUpdate(HexEditorDocumentRAM));
        await Task.Run(() => ForceGetterUpdate(HexEditorDocumentCON));
    }

    private volatile bool _haltCPUClock = false;
    private async void Button_CPURunIndefinitely_OnClick(object? sender, RoutedEventArgs e)
    {
        bool updateMemoryRT = CheckBox_UpdateMemoryRealtime.IsChecked.GetValueOrDefault(false);
        await Task.Run(() =>
        {
            _haltCPUClock = false;
            while (!_haltCPUClock)
            {
                TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
                if (updateMemoryRT) SyncCPUandGUIData();
            }
        });
    }

    private void Button_CPUStop_OnClick(object? sender, RoutedEventArgs e)
    {
        _haltCPUClock = true;
    }

    #endregion User event handlers
}