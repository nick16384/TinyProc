using System.ComponentModel.DataAnnotations;
using TinyProc.Memory;

namespace TinyProc.Processor;

public class CPU
{
    protected Register MAR1 { get; set; } = new(true, RegisterRWAccess.ReadWrite);
    protected Register MAR2 { get; set; } = new(true, RegisterRWAccess.ReadWrite);
    protected Register MDR1 { get; private set; } = new(true, RegisterRWAccess.ReadOnly);
    protected Register MDR2 { get; private set; } = new(true, RegisterRWAccess.ReadOnly);

    Register[] GPRs;

    // TODO: Maybe CU as inner class of CPU?
    ControlUnit CU;
    ALU ALU;

    RawMemory ram;

    public CPU(RawMemory memory)
    {
        this.ram = memory;
    }

    public void NextClock()
    {
        Console.WriteLine("Clock pulse received; Executing next cycle.");
        Subcycle_Fetch();
        Subcycle_Decode();
        Subcycle_Execute();
        Console.WriteLine("Cycle finished. Waiting for next clock pulse.");
    }
    private void Subcycle_Fetch()
    {
        //
    }
    private void Subcycle_Decode()
    {
        // something
    }
    private void Subcycle_Execute()
    {
        // something
    }
}