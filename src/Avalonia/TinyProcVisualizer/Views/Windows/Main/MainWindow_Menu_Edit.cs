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
            ExecutableWrapper programWrapper = new(binaryFilePathToDisassemble);
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
        // Show address range selection dialog
        var memoryRange = await WeakReferenceMessenger.Default.Send(new DisassembleFromRAMMessage());
        uint? startAddress = memoryRange?.DisassemblingStartAddress;
        uint? endAddress = memoryRange?.DisassemblingEndAddress;
        if (!startAddress.HasValue || !endAddress.HasValue)
        {
            Console.WriteLine("User cancelled disassembly memory range selection.");
            return;
        }

        uint[][] memoryDumpFull = TinyProc.Application.ExecutionContainer.INSTANCE0.LiveMemoryDump;
        if ((endAddress - startAddress) % 2 != 0)
            endAddress -= 1;
        uint dumpSize = endAddress.Value - startAddress.Value;
        if (dumpSize > 2_000_000)
        {
            // Delay to prevent the popup appearing behind the window, because the memory range
            // selection dialog has not properly closed yet.
            await Task.Delay(200);
            await MessageBoxManager.GetMessageBoxStandard(
                "Finite memory detected",
                "You tried to disassemble more than 1 million instructions. This app does not agree.\n" +
                "Therefore, this attempt will be aborted. There is no way to circumvent this sanity check.\n" +
                "Please, for the sake of god, think again how much RAM your system has.",
                ButtonEnum.Ok
            ).ShowAsync();

            if (memoryDumpFull.Length > 1)
                Console.Error.WriteLine("Warning: Memory dump has more words than the disassembler can handle at once.");
            return;
        }
        uint[] memoryDump = memoryDumpFull[0];
        // FIXME: If errors occur with big memory sizes, check if the cast from uint to int caused the int to overflow.
        uint[] memoryDisassembleSlice = new uint[(int)dumpSize];
        Array.Copy(memoryDump, (int)startAddress, memoryDisassembleSlice, 0, memoryDisassembleSlice.Length);
        string? previousDisassemblyText = TextBox_SourceAssemblyCodeEditor.Text;
        try
        {
            // TODO: Ideally, show a MessageBox where the user is informed that a disassembly is in progress.
            // this is, however, not possible, because the framework does not provide a way to close a MessageBox via code.
            // See this issue for any updates: https://github.com/AvaloniaCommunity/MessageBox.Avalonia/issues/205
            // This is a half-satisfying workaround currently in use:
            TextBox_SourceAssemblyCodeEditor.Text = "[Disassembly in progress...]";
            string disassembledProgram = "";
            await Task.Run(() => disassembledProgram = TinyProc.Assembling.Assembler.DisassembleFromProgramWithHeader(memoryDisassembleSlice));
            TextBox_SourceAssemblyCodeEditor.Text = disassembledProgram;
        }
        catch (Exception ex)
        {
            TextBox_SourceAssemblyCodeEditor.Text = previousDisassemblyText;
            await MessageBoxManager.GetMessageBoxStandard(
                "Disassembler error",
                $"Disassembler error. Message:\n{ex.Message}\n{ex.InnerException?.Message}\n\nStacktrace:\n{ex.InnerException?.StackTrace}",
                ButtonEnum.Ok).ShowAsync();
            return;
        }
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
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);

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
        TinyProc.Application.ExecutableWrapper programWrapper = new(assembledBinary);

        // Write assembled program to memory
        // Show address selection dialog
        var messageAddress = await WeakReferenceMessenger.Default.Send(new AssembleAndLoadMessage());
        uint? address = messageAddress?.LoadAddress;
        if (!address.HasValue)
        {
            Console.WriteLine("User cancelled assemble and load memory address selection.");
            return;
        }

        if (address + programWrapper.ExecutableProgram.Length
            > TinyProc.Application.ExecutionContainer.INSTANCE0.VirtualMemorySizeWords)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Assembler error",
                $"The program is too large to be loaded at the specified address.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        TinyProc.Application.ExecutionContainer.INSTANCE0.LoadDataAtAddress(programWrapper.ExecutableProgram, address.Value);
    }

    #endregion Edit menu
}