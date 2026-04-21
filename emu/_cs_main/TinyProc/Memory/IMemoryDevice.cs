using TinyProc.Processor;

namespace TinyProc.Memory;

public interface IMemoryDevice : IBusAttachable
{
    public ulong Size { get; }
    public uint ReadDirect(uint addr);
    public bool ReadEnable { get; set; }
}

public interface IReadWriteMemoryDevice : IMemoryDevice
{
    public void WriteDirect(uint addr, uint data);
    public bool WriteEnable { get; set; }
}