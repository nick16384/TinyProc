using TinyProc.Assembling;
using TinyProc.Application;

class Program
{
    // Note: This Main function should only be called when intending to run in CLI mode.
    static void Main(string[] args)
    {
        Logging.SuppressDebugMessages = true;
        Logging.SuppressInfoMessages = false;
        Logging.SuppressWarningMessages = false;
        Logging.SuppressErrorMessages = false;

        Logging.LogInfo(
            $"TinyProc ver. {VersionData.TINYPROC_PROGRAM_VERSION_STR} " +
            $"Processor revision {VersionData.PROCESSOR_REVISION_VERSION_STR}");

        Logging.LogDebug($"Arguments: {args.Length}");

        if (args.Length < 2)
        {
            Logging.LogError(
                "Usage:\n" +
                "--assemble <Source file> [<Target file>] [--raw] [--verbose] : Creates a binary executable file.\n" +
                "--run <Firmware file> <Executable file> [--verbose] : Loads the firmware file into ROM and runs the executable file.");
            return;
        }

        // TODO: Implement some more proper argument parsing and validating with a more thorough usage explanation.
        if (args.Contains("--debug") || args.Contains("-d") || args.Contains("--verbose") || args.Contains("-v"))
        {
            Logging.SuppressDebugMessages = false;
        }

        if (args[0].Equals("--assemble"))
        {
            string sourceFilePath = args[1];
            string? targetFilePath = args.Length >= 3 ? args[2] : null;
            bool outputRaw = args.Length >= 4 && args[3] == "--raw";
            Logging.LogInfo($"Assembling source file {sourceFilePath}");
            if (!sourceFilePath.Trim().EndsWith(".hltp32.asm"))
                Logging.LogWarn("Warning: Source file name does not end with standard suffix \".hltp32.asm\".");

            string assemblyCode = File.ReadAllText(sourceFilePath);
            HLTPExecutable mainProgram = Assembler.Assemble(assemblyCode);

            string outputBinaryFilePath = targetFilePath ?? sourceFilePath + ".bin";
            if (targetFilePath == null && sourceFilePath.EndsWith(".asm"))
                outputBinaryFilePath = sourceFilePath[..^4] + ".bin";
            mainProgram.WriteExecutableBinaryToFile(outputBinaryFilePath, !outputRaw);

            return;
        }

        if (args[0].Equals("--run"))
        {
            string firmwareImagePath = args[1];
            string executablePath = args[2];

            HLTPExecutable programWrapper = HLTPExecutable.LoadProgramFromFile(executablePath);

            Console.CancelKeyPress += delegate
            {
                ExitClean();
            };

            ExecutionContainer.Initialize(firmwareImagePath);
            ExecutionContainer.INSTANCE0.LoadInitialProgram(programWrapper);

            // If this program is at this stage, it is probably running in CLI mode.
            Logging.LogInfo("Program ready to execute. Press enter to start first cycle. List of commands below:");
            Logging.LogInfo ("\"q\":    Exit.");
            Logging.LogInfo ("\"cN\":   Continue for N cycles.");
            Logging.LogDebug("Debug printing enabled. Additional commands below:");
            Logging.LogDebug("\"r\":    Dump registers.");
            Logging.LogDebug("\"mS-E\": Print all addresses between (including both) S and E.");
            Logging.LogDebug("\"s\":    Print all elements of the stack up to (and incl.) the stack pointer.");
            Logging.LogDebug("\"sN\":   Print first N elements of the stack.");
            while (true)
            {
                string? input = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(input))
                {
                    ExecutionContainer.INSTANCE0.StepSingleCycle();
                    continue;
                }
                if (input == "q")
                {
                    // Quit the emulator.
                    break;
                }
                else if (input.StartsWith('c'))
                {
                    Assembler.TryConvertStringToUInt(input[1..], out uint? count);
                    if (!count.HasValue)
                    {
                        Logging.LogDebug("Invalid number of cycles.");
                        continue;
                    }
                    for (uint i = 0; i < count; i++)
                        ExecutionContainer.INSTANCE0.StepSingleCycle();
                }
                else if (input == "r")
                {
                    // Dump all registers without advancing the CPU.
                    Logging.LogDebug("Current register states:");
                    Logging.LogDebug($"GP1: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP1Value:x8}");
                    Logging.LogDebug($"GP2: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP2Value:x8}");
                    Logging.LogDebug($"GP3: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP3Value:x8}");
                    Logging.LogDebug($"GP4: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP4Value:x8}");
                    Logging.LogDebug($"GP5: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP5Value:x8}");
                    Logging.LogDebug($"GP6: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP6Value:x8}");
                    Logging.LogDebug($"GP7: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP7Value:x8}");
                    Logging.LogDebug($"GP8: {ExecutionContainer.INSTANCE0.CPUDebugPort.GP8Value:x8}");
                    Logging.LogDebug($"MAR: {ExecutionContainer.INSTANCE0.CPUDebugPort.MARValue:x8}");
                    Logging.LogDebug($"MDR: {ExecutionContainer.INSTANCE0.CPUDebugPort.MDRValue:x8}");
                    Logging.LogDebug($"IRA: {ExecutionContainer.INSTANCE0.CPUDebugPort.IRAValue:x8}");
                    Logging.LogDebug($"IRB: {ExecutionContainer.INSTANCE0.CPUDebugPort.IRBValue:x8}");
                    Logging.LogDebug($"PC:  {ExecutionContainer.INSTANCE0.CPUDebugPort.PCValue:x8}");
                    Logging.LogDebug($"SR:  {ExecutionContainer.INSTANCE0.CPUDebugPort.SRValue:x8}");
                    Logging.LogDebug($"SP:  {ExecutionContainer.INSTANCE0.CPUDebugPort.SPValue:x8}");
                }
                else if (input.StartsWith('m'))
                {
                    // Print part of memory
                    input = input[1..];
                    if (!input.Contains('-'))
                    {
                        Logging.LogDebug("No start and/or end provided.");
                        continue;
                    }
                    Assembler.TryConvertStringToUInt(input.Split('-')[0], out uint? start);
                    Assembler.TryConvertStringToUInt(input.Split('-')[1], out uint? end);
                    if (!start.HasValue || !end.HasValue)
                    {
                        Logging.LogDebug("Invalid start or end address.");
                        continue;
                    }
                    PrintAddressSpace(ExecutionContainer.INSTANCE0, start.Value, end.Value);
                }
                else if (input.StartsWith('s'))
                {
                    // Print part of the stack
                    // FIXME: This constant needs to be public by MMU and not here and there.
                    const uint STACK_BASE = 0x00020000;
                    if (input.Length > 1)
                    {
                        // User entered number of elements
                        Assembler.TryConvertStringToUInt(input[1..], out uint? num);
                        if (!num.HasValue)
                        {
                            Logging.LogDebug("Invalid number of stack elements.");
                            continue;
                        }
                        PrintAddressSpace(ExecutionContainer.INSTANCE0, STACK_BASE, STACK_BASE + num.Value - 1);
                    }
                    else
                    {
                        // Print stack until SP
                        PrintAddressSpace(ExecutionContainer.INSTANCE0, STACK_BASE, ExecutionContainer.INSTANCE0.CPUDebugPort.SPValue);
                    }
                }
            }
            // End of main loop
            ExitClean();
        }
    }

    /// <summary>
    /// Prints all words stored between the specified addresses (both included).
    /// Also shows the location of the stack pointer if it would be pointing to somewhere in that space.
    /// </summary>
    /// <param name="executionContainer"></param>
    /// <param name="startInclusive"></param>
    /// <param name="endInclusive"></param>
    private static void PrintAddressSpace(ExecutionContainer executionContainer, uint startInclusive, uint endInclusive)
    {
        if (endInclusive < startInclusive)
        {
            Logging.LogDebug("Nothing to print.");
            return;
        }
        for (uint addr = startInclusive; addr <= endInclusive; /* Increment is done separately below */)
        {
            Logging.LogDebug($"{addr:x8}: {executionContainer.ReadVirtualMemDirect(addr):x8}" +
                $"{(addr == executionContainer.CPUDebugPort.SPValue ? " <-- SP" : "")}");
            if (++addr == 0x00000000u)
            {
                Logging.LogDebug("Address increment resulted in overflow, aborting.");
                break;
            }
        }
    }

    private static void ExitClean()
    {
        Logging.LogInfo("Leaving cycle loop and exiting...");
        Console.Out.Flush();
        Console.Error.Flush();
        Environment.Exit(0);
    }
}