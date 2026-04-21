using System;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using TinyProc.Application;
using TinyProcVisualizer.Messages;

namespace TinyProcVisualizer.Views.Windows.Main;

public partial class MainWindow : Window
{
    #region Edit menu

    private async void Menu_Edit_DisassembleFromFile(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Select binary file to disassemble...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file to disassemble: " + files[0].Name);
        string binaryFilePathToDisassemble = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        try
        {
            HLTPExecutable programWrapper = new(binaryFilePathToDisassemble);
            string disassembledProgram = TinyProc.Assembling.Assembler.DisassembleFromProgramWithHeader(programWrapper);
            await Dispatcher.UIThread.InvokeAsync(() => TextBox_SourceAssemblyCodeEditor.Text = disassembledProgram);
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Disassembler error",
                $"Disassembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.InnerException?.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
    }

    private async void Menu_Edit_DisassembleFromRAM(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private async void Menu_Edit_AssembleToFile(object? sender, RoutedEventArgs e)
    {
        string sourceCodeText = TextBox_SourceAssemblyCodeEditor.Text ?? "";
        if (string.IsNullOrWhiteSpace(sourceCodeText))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error",
                $"No source code to assemble.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        // Assemble text to binary executable
        uint[] assembledBinary;
        try
        {
            // Using async await, since the assembling process might take a long time
            assembledBinary = await Task.Run(() => TinyProc.Assembling.Assembler.AssembleToLoadableProgram(sourceCodeText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"Assembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.InnerException?.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.HLTPExecutable programWrapper = new(assembledBinary);

        // Select destination binary file, which stores the output of the assembler.
        var files = await OpenSingleFileSelectionDialog("Open executable target file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Executable target selection cancelled.");
            return;
        }
        Console.WriteLine("Selected executable target file: " + files[0].Name);
        string outputBinaryFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        UpdateHexEditorBinaryExecutableFile(outputBinaryFilePath);
        await Task.Run(() => programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath));
    }

    private async void Menu_Edit_AssembleAndLoadAtAddress(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    #endregion Edit menu
}