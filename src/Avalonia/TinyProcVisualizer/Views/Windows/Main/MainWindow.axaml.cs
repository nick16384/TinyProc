using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHex;
using Avalonia.Controls.ApplicationLifetimes;
using TinyProcVisualizer.ViewModels.Main;
using AvaloniaHex.Rendering;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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

    public void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Console.WriteLine("App exit called.");
        _haltCPUGUIDataSyncThread = true;
        _haltCPUClock = true; // Not necessary
    }

    #region Hex Editors

    private static readonly RealTimeFixedSizeExternalSourceBinaryDocument EMPTY_RT_DOCUMENT = new(() => [], TimeSpan.FromMilliseconds(100));
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_BINARY = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan UPDATE_INTERVAL_DOC_RAM = TimeSpan.FromMilliseconds(200);
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

    private RealTimeFixedSizeExternalSourceBinaryDocument _HexEditorDocumentBinaryExecutableFile = EMPTY_RT_DOCUMENT;
    private RealTimeFixedSizeExternalSourceBinaryDocument HexEditorDocumentBinaryExecutableFile
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
                    = new RealTimeFixedSizeExternalSourceBinaryDocument(() => File.ReadAllBytes(binFilePath), UPDATE_INTERVAL_DOC_BINARY);
            }
            return _HexEditorDocumentBinaryExecutableFile;
        }
    }
    private RealTimeFixedSizeExternalSourceBinaryDocument _HexEditorDocumentRAM = EMPTY_RT_DOCUMENT;
    private RealTimeFixedSizeExternalSourceBinaryDocument HexEditorDocumentRAM
    {
        get
        {
            if (TinyProc.Application.ExecutionContainer.INSTANCE0 == null)
                Console.Error.WriteLine("CPU not initialized yet, cannot read RAM.");
            else
            {
                if ((ulong)TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes.Length != _HexEditorDocumentRAM.Length)
                    ReinitializeDocument(
                        _HexEditorDocumentRAM, () => TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes, UPDATE_INTERVAL_DOC_RAM);
            }
            return _HexEditorDocumentRAM;
        }
    }
    private RealTimeFixedSizeExternalSourceBinaryDocument HexEditorDocumentCON
    {
        get => new(() => [1, 2, 3], TimeSpan.FromMilliseconds(100));
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
    private static void ReinitializeDocument(
        RealTimeFixedSizeExternalSourceBinaryDocument document, Func<ReadOnlySpan<byte>> backingSource, TimeSpan updateInterval)
    {
        // Reinitialize document when e.g. the size changed
        document = new RealTimeFixedSizeExternalSourceBinaryDocument(backingSource, updateInterval);
        // Exclude backing updates for bytes that have been changed by the user
        document.Changed += (sender, eventArgs) => document.AddLockedRange(eventArgs.AffectedRange);
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

        ReinitializeDocument(_HexEditorDocumentRAM, () => TinyProc.Application.ExecutionContainer.INSTANCE0.LiveRAMBytes, UPDATE_INTERVAL_DOC_RAM);

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

    private void Button_HexEditor1_OverrideMemoryContents(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Button_HexEditor1_RefreshMemoryContents(object? sender, RoutedEventArgs e)
    {
        if (HexEditor1.Document is RealTimeFixedSizeExternalSourceBinaryDocument document)
            document.ResetUpdateRanges();
        else
            Console.Error.WriteLine("Hex editor 1 document is not an RT document; Cannot refresh.");
    }

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