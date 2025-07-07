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
using TinyProcVisualizer.ViewModels.Main;
using AvaloniaHex.Rendering;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TinyProc.Application;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using TinyProcVisualizer.Messages;
using TinyProcVisualizer.Views.Windows.Dialog_DisassembleFromRAM;
using TinyProcVisualizer.ViewModels.Dialog_DisassembleFromRAM;
using TinyProcVisualizer.Views.Windows.Dialog_AssembleAndLoad;
using TinyProcVisualizer.ViewModels.Dialog_AssembleAndLoad;

namespace TinyProcVisualizer.Views.Windows.Main;

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

        // Register handlers for Windows that ask the user to enter data.
        // Example cause I don't understand this crap:
        // https://docs.avaloniaui.net/docs/tutorials/music-store-app/opening-a-dialog
        WeakReferenceMessenger.Default.Register<MainWindow, DisassembleFromRAMMessage>(this, static (window, message) =>
        {
            var dialog = new DialogDisassembleFromRAM
            {
                DataContext = new DialogDisassembleFromRAM_ViewModel()
            };
            message.Reply(dialog.ShowDialog<DialogDisassembleFromRAM_ViewModel?>(window));
        });
        WeakReferenceMessenger.Default.Register<MainWindow, AssembleAndLoadMessage>(this, static (window, message) =>
        {
            var dialog = new DialogAssembleAndLoad
            {
                DataContext = new DialogAssembleAndLoad_ViewModel()
            };
            message.Reply(dialog.ShowDialog<DialogAssembleAndLoad_ViewModel?>(window));
        });

        // Initialize event handlers responsible primarily for resizing window elements.
        // Note, that these handlers apply scaling more specific than what could be done
        // otherwise in the AXAML.
        InitWindowScalingAndPositioning();

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

    private string? executableTargetPath;
    private string? _binaryExecutableFilePath;

    #region Toolbar

    #region File menu

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

    private async void Menu_File_AssemblySourceFileSelectAndAssemble_OnClick(object? sender, RoutedEventArgs e)
    {
        // Load assembly file
        var files = await OpenSingleFileSelectionDialog("Open Assembly Source Code file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Assembly source file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected assembly source file: " + files[0].Name);
        executableTargetPath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);

        // Assemble loaded assembly file
        // Read assembly source file contents
        string? sourceFilePath = executableTargetPath;
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                "No assembly file selection found. Aborting assembling process.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        string sourceFileText = File.ReadAllText(sourceFilePath);

        // Assemble source file to binary and save to binary file
        uint[] assembledBinary;
        try
        {
            // Using async await, since the assembling process might take a long time
            assembledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceFileText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"Assembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);
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

    #endregion File menu

    #region Edit menu

    private async void Menu_Edit_DisassembleFromFile(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Select binary file to disassemble...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file to disassemble: " + files[0].Name);
        string binaryFilePathToDisassemble = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        try
        {
            ExecutableWrapper programWrapper = new(binaryFilePathToDisassemble);
            string disassembledProgram = TinyProc.Assembler.Assembler.DisassembleFromProgramWithHeader(programWrapper);
            await Dispatcher.UIThread.InvokeAsync(() => TextBox_SourceAssemblyCodeEditor.Text = disassembledProgram);
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Disassembler error",
                $"Disassembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
    }

    private async void Menu_Edit_DisassembleFromRAM(object? sender, RoutedEventArgs e)
    {
        // Show address range selection dialog
        var memoryRange = await WeakReferenceMessenger.Default.Send(new DisassembleFromRAMMessage());
        uint? startAddress = memoryRange?.DisassemblingStartAddress;
        uint? endAddress = memoryRange?.DisassemblingEndAddress;
        if (!startAddress.HasValue || !endAddress.HasValue)
        {
            Console.WriteLine("User cancelled disassembly memory range selection.");
            return;
        }

        uint[][] memoryDumpFull = TinyProc.Application.ExecutionContainer.INSTANCE0.LiveMemoryDump;
        if ((endAddress - startAddress) % 2 != 0)
            endAddress -= 1;
        uint dumpSize = endAddress.Value - startAddress.Value;
        if (dumpSize > 2_000_000)
        {
            // Delay to prevent the popup appearing behind the window, because the memory range
            // selection dialog has not properly closed yet.
            await Task.Delay(200);
            await MessageBoxManager.GetMessageBoxStandard(
                "Finite memory detected",
                "You tried to disassemble more than 1 million instructions. This app does not agree.\n" +
                "Therefore, this attempt will be aborted. There is no way to circumvent this sanity check.\n" +
                "Please, for the sake of god, think again how much RAM your system has.",
                ButtonEnum.Ok
            ).ShowAsync();

            if (memoryDumpFull.Length > 1)
                Console.Error.WriteLine("Warning: Memory dump has more words than the disassembler can handle at once.");
            return;
        }
        uint[] memoryDump = memoryDumpFull[0];
        // FIXME: If errors occur with big memory sizes, check if the cast from uint to int caused the int to overflow.
        uint[] memoryDisassembleSlice = new uint[(int)dumpSize];
        Array.Copy(memoryDump, (int)startAddress, memoryDisassembleSlice, 0, memoryDisassembleSlice.Length);
        string? previousDisassemblyText = TextBox_SourceAssemblyCodeEditor.Text;
        try
        {
            // TODO: Ideally, show a MessageBox where the user is informed that a disassembly is in progress.
            // this is, however, not possible, because the framework does not provide a way to close a MessageBox via code.
            // See this issue for any updates: https://github.com/AvaloniaCommunity/MessageBox.Avalonia/issues/205
            // This is a half-satisfying workaround currently in use:
            TextBox_SourceAssemblyCodeEditor.Text = "[Disassembly in progress...]";
            string disassembledProgram = "";
            await Task.Run(() => disassembledProgram = TinyProc.Assembler.Assembler.DisassembleFromProgramWithHeader(memoryDisassembleSlice));
            TextBox_SourceAssemblyCodeEditor.Text = disassembledProgram;
        }
        catch (Exception ex)
        {
            TextBox_SourceAssemblyCodeEditor.Text = previousDisassemblyText;
            await MessageBoxManager.GetMessageBoxStandard(
                "Disassembler error",
                $"Disassembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
    }

    private async void Menu_Edit_AssembleToFile(object? sender, RoutedEventArgs e)
    {
        string sourceCodeText = TextBox_SourceAssemblyCodeEditor.Text ?? "";
        if (string.IsNullOrWhiteSpace(sourceCodeText))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                $"No source code to assemble.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        // Assemble text to binary executable
        uint[] assembledBinary;
        try
        {
            // Using async await, since the assembling process might take a long time
            assembledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceCodeText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"Assembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);

        // Select destination binary file, which stores the output of the assembler.
        var files = await OpenSingleFileSelectionDialog("Open executable target file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Executable target selection cancelled.");
            return;
        }
        Console.WriteLine("Selected executable target file: " + files[0].Name);
        executableTargetPath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        await Task.Run(() => programWrapper.WriteExecutableBinaryToFile(executableTargetPath));
    }

    private async void Menu_Edit_AssembleAndLoadAtAddress(object? sender, RoutedEventArgs e)
    {
        //string 

        string sourceCodeText = TextBox_SourceAssemblyCodeEditor.Text ?? "";
        if (string.IsNullOrWhiteSpace(sourceCodeText))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                $"No source code to assemble.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        // Assemble text to binary executable
        uint[] assembledBinary;
        try
        {
            // Using async await, since the assembling process might take a long time
            assembledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceCodeText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"Assembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);

        // Write assembled program to memory

    }

    #endregion Edit menu

    #endregion Toolbar

    #region Logging

    private void CheckBox_LogDebugMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressDebugMessages = !CheckBox_LogDebugMessages.IsChecked.GetValueOrDefault(true);
    private void CheckBox_LogInfoMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressInfoMessages = !CheckBox_LogInfoMessages.IsChecked.GetValueOrDefault(true);
    private void CheckBox_LogWarningMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressWarningMessages = !CheckBox_LogWarningMessages.IsChecked.GetValueOrDefault(true);
    private void CheckBox_LogErrorMessages_OnClick(object? sender, RoutedEventArgs e)
        => TinyProc.Application.Logging.SuppressErrorMessages = !CheckBox_LogErrorMessages.IsChecked.GetValueOrDefault(true);

    
    #endregion Logging
    
    #endregion User event handlers
}