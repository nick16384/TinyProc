using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;

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
                SyncCPUandGUIData();
            }
        });
    }

    private static readonly TimeSpan SYNC_INTERVAL_CPU_GUI = TimeSpan.FromMilliseconds(200);
    private static volatile bool _haltCPUGUIDataSyncThread = false;

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
        string gp1Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP1Value:X8}";
        string gp2Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP2Value:X8}";
        string gp3Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP3Value:X8}";
        string gp4Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP4Value:X8}";
        string gp5Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP5Value:X8}";
        string gp6Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP6Value:X8}";
        string gp7Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP7Value:X8}";
        string gp8Value = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_GP8Value:X8}";
        string pcValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_PCValue:X8}";
        string srValue = $"{TinyProc.Application.ExecutionContainer.INSTANCE0.Debug_CPU_SRValue:X8}";
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
                updateTextBlock_SR.GetTask(),
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
    }

    #endregion CPU -> GUI data synchronization
}