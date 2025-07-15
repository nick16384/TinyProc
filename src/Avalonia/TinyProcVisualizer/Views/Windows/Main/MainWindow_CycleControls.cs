using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace TinyProcVisualizer.Views.Windows.Main;

public partial class MainWindow : Window
{
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

    private void Button_CPURunIndefinitely_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntil(() => _haltCPUClock, true);

    private void Button_CPUFastForwardIndefinitely_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntil(() => _haltCPUClock);

    private async void Button_CPUStop_OnClick(object? sender, RoutedEventArgs e)
    {
        _haltCPUClock = true;
        // If the CPU is in cycle sleep, cause it to cancel (to avoid very long waiting times)
        cpuRunTaskCancellationTokenSource.Cancel();
    }

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
                "Unable to parse memory address and/or memory value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (NullReferenceException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Empty memory address and/or value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (OverflowException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Memory address or value overflowed (must by <= 2^32)",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        RunCPUUntil(() =>
            TinyProc.Application.ExecutionContainer.INSTANCE0.ReadRAMDirect(memAddress) == memValueRequired,
            updateGUIInRealtime);
    }
    public static uint ConvertStringToUInt(string numStr)
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
                "Unable to parse number of cycles to run for",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (NullReferenceException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Empty number of cycles to run for",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (OverflowException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Cycle number overflowed (must by <= 2^32)",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        RunCPUUntil(() =>
            TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle - startCycleCount >= cyclesToRun,
            updateGUIInRealtime);
    }

    private void Button_CPURunUntil_RegEqValue_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntilRegisterHasValue(true);
    private void Button_CPUFFUntil_RegEqValue_OnClick(object? sender, RoutedEventArgs e)
        => RunCPUUntilRegisterHasValue(false);
    private async void RunCPUUntilRegisterHasValue(bool updateGUIInRealtime)
    {
        string registerName;
        uint valueRequired;
        try
        {
            registerName = (ComboBox_CPURunUntil_RegEqValue_Register.SelectedItem as ComboBoxItem).Content as string;
            // TODO: Implement autoconversion from string literals to binary
            valueRequired = ConvertStringToUInt(TextBox_CPURunUntil_RegEqValue_Value.Text);
        }
        catch (FormatException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Unable to parse desired register value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (NullReferenceException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Empty register value",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        catch (OverflowException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                "Register value overflowed (must by <= 2^32)",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        switch (registerName)
        {
            case "GPR 1":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP1Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 2":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP2Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 3":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP3Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 4":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP4Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 5":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP5Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 6":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP6Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 7":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP7Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "GPR 8":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.GP8Value == valueRequired,
                    updateGUIInRealtime);
                break;
            case "PC":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue == valueRequired,
                    updateGUIInRealtime);
                break;
            case "IRA":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRAValue == valueRequired,
                    updateGUIInRealtime);
                break;
            case "IRB":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.IRBValue == valueRequired,
                    updateGUIInRealtime);
                break;
            case "SR":
                RunCPUUntil(() =>
                    TinyProc.Application.ExecutionContainer.INSTANCE0.CPUDebugPort.SRValue == valueRequired,
                    updateGUIInRealtime);
                break;
        }
    }

    private volatile bool _isCPURunning = false;
    private volatile bool _haltCPUClock = false;
    CancellationTokenSource cpuRunTaskCancellationTokenSource = new();
    private async void RunCPUUntil(Func<bool> haltCondition, bool updateGUIInRealtime = false)
    {
        if (_isCPURunning)
        {
            Console.Error.WriteLine("Cannot run CPU clock: Already running.");
            return;
        }
        _isCPURunning = true;
        if (TinyProc.Application.ExecutionContainer.INSTANCE0.IsCPUInInvalidState)
        {
            Console.Error.WriteLine("Cannot run CPU clock: CPU in invalid state.");
            return;
        }
        Thickness previousTextBoxBorderThickness = TextBox_CurrentCPUCycle.BorderThickness;
        IBrush? previousTextBoxBorderBrush = TextBox_CurrentCPUCycle.BorderBrush;
        TextBox_CurrentCPUCycle.BorderThickness = Thickness.Parse("2.0");
        TextBox_CurrentCPUCycle.BorderBrush = Brushes.Lime;
        TextBox_ClockRate.BorderThickness = Thickness.Parse("2.0");
        TextBox_ClockRate.BorderBrush = Brushes.Lime;

        if (!updateGUIInRealtime)
            TextBox_CycleTimeGUIOverhead.Text = "-";

        uint requestedCycleSleepMillis = 0;
        bool sleepIncludeCycleTime = false;
        try
        {
            requestedCycleSleepMillis = ConvertStringToUInt(TextBox_CycleSleep_SleepTime.Text);
            sleepIncludeCycleTime = CheckBox_CycleSleep_IncludeRuntime.IsChecked.GetValueOrDefault(false);
        }
        catch (Exception) { }

        _haltCPUClock = false;


        Stopwatch cycleStopwatch = new();
        Task cpuRunTask = Task.Run(async () =>
        {
            while (!haltCondition() && !_haltCPUClock && !TinyProc.Application.ExecutionContainer.INSTANCE0.IsCPUInInvalidState)
            {
                TinyProc.Application.ExecutionContainer.INSTANCE0.StepSingleCycle();
                // Ignoring cycle runtime, since it is comparatively low to the GUI overhead
                if (updateGUIInRealtime)
                {
                    cycleStopwatch.Restart();
                    SyncCPUandGUIData();
                    UpdateGUIOverheadTimeTextBox(cycleStopwatch.Elapsed);
                }
                // Sleep for specified amount before executing next cycle.
                // The accuracy of this Sleep() call is low compared to millisecond-accurate timers.
                // The reasons for this are artifacts of preemptive OS process scheduling and not a topic
                // of further discussion here.
                // Note, however, that for this reason, there is no need to measure the cycle time
                // more accurate than in milliseconds, since the scheduling process introduces by far
                // the largest error margin, which usually lies in the single digit millisecond range.
                long sleepTimeMillis = Math.Max(0,
                    requestedCycleSleepMillis - (sleepIncludeCycleTime ? cycleStopwatch.ElapsedMilliseconds : 0));
                await Task.Delay((int)sleepTimeMillis, cpuRunTaskCancellationTokenSource.Token);
            }
        });
        try { await cpuRunTask; }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine("CPU execution prematurely canceled. It may be remaining in an unstable state!");
            cpuRunTaskCancellationTokenSource = new CancellationTokenSource();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("CPU runtime exception");
            _haltCPUGUIDataSyncThread = true;
            await MessageBoxManager.GetMessageBoxStandard(
                "CPU runtime exception",
                "A CPU runtime exception occurred.\n" +
                "The CPU has now entered an invalid state, in which it is unable to execute any further instructions.\n\n" +
                $"Stacktrace:\n{ex.Message}\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            previousTextBoxBorderThickness = new Thickness(2.0);
            previousTextBoxBorderBrush = Brushes.Red;
        }
        _isCPURunning = false;
        TextBox_CurrentCPUCycle.BorderThickness = previousTextBoxBorderThickness;
        TextBox_CurrentCPUCycle.BorderBrush = previousTextBoxBorderBrush;
        TextBox_ClockRate.BorderThickness = previousTextBoxBorderThickness;
        TextBox_ClockRate.BorderBrush = previousTextBoxBorderBrush;
    }

    #endregion CPU cycle controls
}