using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using TinyProcVisualizer.ViewModels;

namespace TinyProcVisualizer.Views;

public partial class MainWindow : Window
{
    // Code responsible for scaling / sizing elements in a way, that is not directly possible in the AXAML.

    private const uint GRID_TOOLBAR_HEIGHT = 30;

    private const uint GRID_ADVANCED_CYCLE_CONTROL_WIDTH = 365;
    private const uint GRID_ADVANCED_CYCLE_CONTROL_HEIGHT = 95;
    private const uint GRID_ADVANCED_CYCLE_CONTROL_TEXTBOX_WIDTH = 90;

    private const uint SCROLLVIEWER_SOURCECODEEDITOR_MARGIN_LEFT = 5;
    private const uint SCROLLVIEWER_SOURCECODEEDITOR_MARGIN_RIGHT = 5;
    private const uint SCROLLVIEWER_SOURCECODEEDITOR_HEIGHT = 300;

    private const uint HEXEDITOR1_WIDTH = 365;
    private const uint HEXEDITOR1_HEIGHT = 440;
    private const uint HEXEDITOR2_WIDTH = HEXEDITOR1_WIDTH;
    private const uint HEXEDITOR2_HEIGHT = HEXEDITOR1_HEIGHT;

    private const uint REGISTER_TEXTBLOCK_WIDTH = 80;

    private bool _initWindowScalingEventHandlersHasBeenCalled = false;
    private void InitWindowScalingAndPositioning()
    {
        // Scaling event handlers are mostly called when the window resizes and periodically.

        if (_initWindowScalingEventHandlersHasBeenCalled)
        {
            Console.Error.WriteLine("Init window scaling event handlers called twice or more. Ignoring this and any subsequent calls.");
            return;
        }
        _initWindowScalingEventHandlersHasBeenCalled = true;

        // Dynamic updaters
        this.LayoutUpdated += (_, _) =>
        {
            MainWindowViewModel.Window_Width = (int)this.Bounds.Width;
            MainWindowViewModel.Window_Height = (int)this.Bounds.Height;
        };
        Canvas_MainWindowCanvas.LayoutUpdated += (_, _) =>
        {
            Canvas_MainWindowCanvas.MinWidth = MainWindowViewModel.Window_Width;
            Canvas_MainWindowCanvas.MinHeight = MainWindowViewModel.Window_Height;
        };
        TextBox_CycleSleep_SleepTime.LayoutUpdated += (_, _) =>
        {
            TextBox_CycleSleep_SleepTime.Width = CheckBox_CycleSleep_IncludeRuntime.Bounds.Width;
        };

        // Source (assembly) code editor
        // This is the only element that resizes dynamically depending on the window size.
        ScrollViewer_SourceCodeEditor.LayoutUpdated += (_, _) =>
        {
            ScrollViewer_SourceCodeEditor.Margin = new Thickness(SCROLLVIEWER_SOURCECODEEDITOR_MARGIN_LEFT, 0, 0, 0);
            ScrollViewer_SourceCodeEditor.Width =
                Math.Max(0,
                // Get the window width and then subtract the widths of all elements, that the source code editor is put between in.
                MainWindowViewModel.Window_Width
                - Grid_CPUControlAndStatus.Bounds.Width
                - Grid_HexEditors.Bounds.Width
                - Grid_CPUControlAndStatus.Margin.Left - Grid_CPUControlAndStatus.Margin.Right
                - Grid_HexEditors.Margin.Left - Grid_HexEditors.Margin.Right
                - Grid_CPUControlAndStatus.Children.Select(child => child.Margin.Left + child.Margin.Right).Sum()
                - Grid_HexEditors.Children.Select(child => child.Margin.Left + child.Margin.Right).Sum()
                // Subtract additional right margin
                - SCROLLVIEWER_SOURCECODEEDITOR_MARGIN_RIGHT
                - SCROLLVIEWER_SOURCECODEEDITOR_MARGIN_LEFT
                );
            ScrollViewer_SourceCodeEditor.Height = SCROLLVIEWER_SOURCECODEEDITOR_HEIGHT;
        };

        // Static updates (only called once)
        // Toolbar
        Grid_Toolbar.Height = GRID_TOOLBAR_HEIGHT;

        // Advanced cycle control panel + Cycle sleep
        // Set grid bounds
        Grid_AdvancedCycleControl.Width = GRID_ADVANCED_CYCLE_CONTROL_WIDTH;
        Grid_AdvancedCycleControl.Height = GRID_ADVANCED_CYCLE_CONTROL_HEIGHT;

        // Set bounds of specific grid rows / columns
        Grid_AdvancedCycleControl.ColumnDefinitions[3].Width = new GridLength(GRID_ADVANCED_CYCLE_CONTROL_TEXTBOX_WIDTH);
        Grid_AdvancedCycleControl.ColumnDefinitions[5].Width = new GridLength(GRID_ADVANCED_CYCLE_CONTROL_TEXTBOX_WIDTH);

        // Hex editors
        // Note to self: If "Width" causes problems, try "MinWidth"
        HexEditor1.Width = HEXEDITOR1_WIDTH;
        HexEditor1.Height = HEXEDITOR1_HEIGHT;
        HexEditor2.Width = HEXEDITOR2_WIDTH;
        HexEditor2.Height = HEXEDITOR2_HEIGHT;

        // Registers
        // Special-purpose registers
        Grid_SpecialPurposeRegisters.Height = Grid_SpecialPurposeRegisters.Children.Select(child => child.Height).Sum();
        Grid_SpecialPurposeRegisters.ColumnDefinitions[1].Width = new GridLength(REGISTER_TEXTBLOCK_WIDTH);
        Grid_SpecialPurposeRegisters.ColumnDefinitions[2].Width = new GridLength(REGISTER_TEXTBLOCK_WIDTH);
        // GPRs
        Grid_GeneralPurposeRegisters.Height = Grid_GeneralPurposeRegisters.Children.Select(child => child.Height).Sum();
        Grid_GeneralPurposeRegisters.ColumnDefinitions[1].Width = new GridLength(REGISTER_TEXTBLOCK_WIDTH);
    }
}