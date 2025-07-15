using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TinyProcVisualizer.ViewModels.Dialog_AssembleAndLoad;
using TinyProcVisualizer.Views.Windows.Main;

namespace TinyProcVisualizer.Views.Windows.Dialog_AssembleAndLoad;

public partial class DialogAssembleAndLoad : Window
{
    public DialogAssembleAndLoad()
    {
        InitializeComponent();
    }
    
    private static uint LowestAddress { get => 0x00000000u; }
    private static uint HighestAddress { get => TinyProc.Application.ExecutionContainer.INSTANCE0.VirtualMemorySizeWords - 1; }

    private void Button_SetMinLoadAddress_OnClick(object? sender, RoutedEventArgs e)
    {
        TextBox_LoadAddress.Text = $"0x{LowestAddress:X8}";
    }

    private async void Button_SetMaxLoadAddress_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            TextBox_LoadAddress.Text = $"0x{HighestAddress:X8}";
        }
        catch (Exception)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                $"Unable to determine highest address. CPU probably not initialized yet.",
                ButtonEnum.Ok).ShowAsync();
        }
    }

    private async void Button_SubmitLoadAddress_OnClick(object? sender, RoutedEventArgs e)
    {
        string? loadAddressStr = TextBox_LoadAddress.Text;
        uint loadAddress;
        try
        {
            loadAddress = MainWindow.ConvertStringToUInt(loadAddressStr);
        }
        catch (Exception)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Parse error",
                $"Cannot parse load address.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        if (this.DataContext is DialogAssembleAndLoad_ViewModel viewModel)
        {
            viewModel.LoadAddress = loadAddress;
            Close(viewModel);
        }
    }
}