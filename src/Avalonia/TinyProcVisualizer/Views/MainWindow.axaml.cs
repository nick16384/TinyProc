using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "TinyProc CPU Emulator Visualizer v" + TinyProc.ApplicationGlobal.GlobalData.TINYPROC_VERSION_STR;
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