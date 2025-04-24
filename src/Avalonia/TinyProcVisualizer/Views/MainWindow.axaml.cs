using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Buttonus!");
        Test1TB.Text = "lel";
    }

    private void Button_CPUInit_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Initializing new CPU");
    }
}