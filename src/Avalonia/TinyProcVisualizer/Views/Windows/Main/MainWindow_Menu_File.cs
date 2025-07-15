using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace TinyProcVisualizer.Views.Windows.Main;

public partial class MainWindow : Window
{
    private string? _executableTargetPath;
    private string? _binaryExecutableFilePath;

    #region File menu

    private async Task<IReadOnlyList<IStorageFile>> OpenSingleFileSelectionDialog(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        return files;
    }

    private async void Menu_File_AssemblySourceFileSelectAndAssemble_OnClick(object? sender, RoutedEventArgs e)
    {
        // Load assembly file
        var files = await OpenSingleFileSelectionDialog("Open Assembly Source Code file...");

        if (files.Count <= 0)
        {
            Console.WriteLine("Assembly source file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected assembly source file: " + files[0].Name);
        _executableTargetPath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);

        // Assemble loaded assembly file
        // Read assembly source file contents
        string? sourceFilePath = _executableTargetPath;
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                "No assembly file selection found. Aborting assembling process.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        string sourceFileText = File.ReadAllText(sourceFilePath);

        // Assemble source file to binary and save to binary file
        uint[] assembledBinary;
        try
        {
            // Using async await, since the assembling process might take a long time
            assembledBinary = await Task.Run(() => TinyProc.Assembler.Assembler.AssembleToMachineCode(sourceFileText));
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"Assembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);
        string outputBinaryFilePath = sourceFilePath + ".bin";
        if (sourceFilePath.EndsWith(".asm"))
            outputBinaryFilePath = sourceFilePath[..^4] + ".bin";
        await Task.Run(() => programWrapper.WriteExecutableBinaryToFile(outputBinaryFilePath));

        // Set binary file in GUI
        _binaryExecutableFilePath = outputBinaryFilePath;
        ReloadHexEditorDocuments();
    }
    private async void Menu_File_BinaryExecutableFileSelect_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await OpenSingleFileSelectionDialog("Open Binary Executable file...");
        if (files.Count <= 0)
        {
            Console.WriteLine("Binary file selection cancelled.");
            return;
        }
        Console.WriteLine("Selected binary executable file: " + files[0].Name);
        _binaryExecutableFilePath = HttpUtility.UrlDecode(files[0].Path.AbsolutePath);
        ReloadHexEditorDocuments();
    }

    #endregion File menu
}