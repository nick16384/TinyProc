using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    #region CPU -> GUI data synchronization

    // TODO: This could maybe be made more beautiful
    // Thread that periodically synchronizes the data from the CPU (ExecutionContainer)
    // with the data shown on screen to the user via the GUI.
    private Thread CPUGUIDataSyncThread;
    private void InitCPUGUIDataSyncThread()
    {
        CPUGUIDataSyncThread = new(() =>
        {
            _haltCPUGUIDataSyncThread = false;
            while (!_haltCPUGUIDataSyncThread)
            {
                Thread.Sleep(SYNC_INTERVAL_CPU_GUI);
                try { SyncCPUandGUIData(); }
                catch (Exception)
                {
                    Console.Error.WriteLine("CPU -> GUI Data sync failed.");
                }
            }
        });
    }

    private static readonly TimeSpan SYNC_INTERVAL_CPU_GUI = TimeSpan.FromMilliseconds(200);
    private static volatile bool _haltCPUGUIDataSyncThread = false;

    private void SyncCPUandGUIData()
    {
        // Nomenclature:
        // Sync: Data from the CPU is fetched and stored in this class
        // Update: The updated local data is updated visually in the GUI

        // Check if the CPU is already initialized. If not, synchronization is unnecessary.
        if (TinyProc.Application.ExecutionContainer.INSTANCE0 == null)
            return;

        // TODO: Check if the CPU cycle has finished before updating

        // Sync & update current CPU cycle TextBox
        string currentCPUCycle = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle:N0}";
        var updateTextBox_CurrentCPUCycle = Dispatcher.UIThread.InvokeAsync(() => TextBox_CurrentCPUCycle.Text = currentCPUCycle);

        // Sync and update last CPU cycle time
        string lastCycleTime = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.LastCycleTimeMicroseconds:N0}us";
        var updateTextBox_LastCycleTime = Dispatcher.UIThread.InvokeAsync(() => TextBox_LastCPUCycleTime.Text = lastCycleTime);

        // Sync and update register text blocks
        // General-purpose registers
        string gp1Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP1Value:X8}";
        string gp2Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP2Value:X8}";
        string gp3Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP3Value:X8}";
        string gp4Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP4Value:X8}";
        string gp5Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP5Value:X8}";
        string gp6Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP6Value:X8}";
        string gp7Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP7Value:X8}";
        string gp8Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP8Value:X8}";
        // Special registers
        string pcValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue:X8}";
        string iraValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRAValue:X8}";
        string irbValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRBValue:X8}";
        string srValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.SRValue:X8}";
        string marValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue:X8}";
        string mdrValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MDRValue:X8}";
        // General-purpose registers
        var updateTextBlock_GP1 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR1.Text = gp1Value);
        var updateTextBlock_GP2 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR2.Text = gp2Value);
        var updateTextBlock_GP3 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR3.Text = gp3Value);
        var updateTextBlock_GP4 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR4.Text = gp4Value);
        var updateTextBlock_GP5 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR5.Text = gp5Value);
        var updateTextBlock_GP6 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR6.Text = gp6Value);
        var updateTextBlock_GP7 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR7.Text = gp7Value);
        var updateTextBlock_GP8 = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterGPR8.Text = gp8Value);
        // Special registers
        var updateTextBlock_PC = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterPC.Text = pcValue);
        var updateTextBlock_IRA = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterIRA.Text = iraValue);
        var updateTextBlock_IRB = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterIRB.Text = irbValue);
        var updateTextBlock_SR = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterSR.Text = srValue);
        var updateTextBlock_MAR = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterMAR.Text = marValue);
        var updateTextBlock_MDR = Dispatcher.UIThread.InvokeAsync(() => TextBlock_RegisterMDR.Text = mdrValue);

        // Sync and update register address highlighters
        HexEditorPCHighlighter.Ranges.Clear();
        HexEditorMARHighlighter.Ranges.Clear();
        ulong pcStartByte = TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue * sizeof(uint);
        ulong pcEndByte = (TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue + 2) * sizeof(uint);
        HexEditorPCHighlighter.Ranges.Add(new BitRange(pcStartByte, pcEndByte));
        ulong marStartByte = TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue * sizeof(uint);
        ulong marEndByte = (TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue + 1) * sizeof(uint);
        HexEditorMARHighlighter.Ranges.Add(new BitRange(marStartByte, marEndByte));
        var updateHexEditor1Highlight = Dispatcher.UIThread.InvokeAsync(HexEditor1.HexView.InvalidateVisualLines);
        var updateHexEditor2Highlight = Dispatcher.UIThread.InvokeAsync(HexEditor2.HexView.InvalidateVisualLines);

        // Sync RAM and CON hex view (They update themselves)
        var syncRAM = Task.Run(() => ForceGetterUpdate(HexEditorDocumentRAM));
        var syncCON = Task.Run(() => ForceGetterUpdate(HexEditorDocumentCON));

        // Wait for all update tasks to complete
        try
        {
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
                updateTextBlock_IRA.GetTask(),
                updateTextBlock_IRB.GetTask(),
                updateTextBlock_SR.GetTask(),
                updateTextBlock_MAR.GetTask(),
                updateTextBlock_MDR.GetTask(),
                updateHexEditor1Highlight.GetTask(),
                updateHexEditor2Highlight.GetTask(),
                syncRAM,
                syncCON
            ]);
        }
        catch (AggregateException ae)
        {
            // Only rethrow the exception if any of the inner exceptions is not a TaskCanceledException.
            // Otherwise, the exception was probably caused by the user exiting the app and therefore cancelling all tasks.
            foreach (Exception exception in ae.InnerExceptions)
                if (exception.GetType() != typeof(TaskCanceledException))
                    throw;
        }
        catch (TaskCanceledException)
        {
            // Same as above, only rethrow if not a TaskCanceledException, so do nothing here.
        }
    }

    #endregion CPU -> GUI data synchronization
}