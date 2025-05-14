using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"TinyProc CPU Emulator v{TinyProc.ApplicationGlobal.GlobalData.TINYPROC_PROGRAM_VERSION_STR} Visualizer";
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Buttonus!");
        Test1TB.Text = "lel";
        InitCPU.IsEnabled = false;
    }

    private void Button_CPUInit_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
    }
}