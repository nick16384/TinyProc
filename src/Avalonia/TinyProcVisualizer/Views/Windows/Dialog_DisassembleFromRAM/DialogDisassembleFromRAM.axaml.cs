using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TinyProcVisualizer.ViewModels.Dialog_DisassembleFromRAM;
using TinyProcVisualizer.Views.Windows.Main;

namespace TinyProcVisualizer.Views.Windows.Dialog_DisassembleFromRAM;

public partial class DialogDisassembleFromRAM : Window
{
    public DialogDisassembleFromRAM()
    {
        InitializeComponent();

        // Apply uniform padding to all grid children
        foreach (var child in Grid_DisassemblingAddressRanges.Children)
            if (child.Margin.Top == 0 && child.Margin.Bottom == 0 && child.Margin.Left == 0 && child.Margin.Right == 0)
                child.Margin = new Avalonia.Thickness(2);
    }

    private static uint LowestAddress { get => 0x00000000u; }
    private static uint HighestAddress { get => TinyProc.Application.ExecutionContainer.INSTANCE0.VirtualMemorySizeWords - 1; }

    private void Button_SetMinStartAddress_OnClick(object? sender, RoutedEventArgs e)
    {
        TextBox_DisassemblingStartAddress.Text = $"0x{LowestAddress:X8}";
    }

    private async void Button_SetMaxEndAddress_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            TextBox_DisassemblingEndAddress.Text = $"0x{HighestAddress:X8}";
        }
        catch (Exception)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                $"Unable to determine highest address. CPU probably not initialized yet.",
                ButtonEnum.Ok).ShowAsync();
        }
    }

    private async void Button_SubmitAddressRanges_OnClick(object? sender, RoutedEventArgs e)
    {
        string? startAddressStr = TextBox_DisassemblingStartAddress.Text;
        string? endAddressStr = TextBox_DisassemblingEndAddress.Text;
        uint startAddress;
        uint endAddress;
        try
        {
            startAddress = MainWindow.ConvertStringToUInt(startAddressStr);
            endAddress = MainWindow.ConvertStringToUInt(endAddressStr);
        }
        catch (Exception)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Cannot parse contents of either start or end address.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        if (this.DataContext is DialogDisassembleFromRAM_ViewModel viewModel)
        {
            viewModel.DisassemblingStartAddress = startAddress;
            viewModel.DisassemblingEndAddress = endAddress;
            Close(viewModel);
        }
    }
}