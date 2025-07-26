namespace TinyProc.Assembling.Sections;

public interface IAssemblySection
{
    public uint Size { get; }
    public bool IsRelocatable { get; }
    public uint? FixedLoadAddress { get; }
    public List<uint> BinaryRepresentation { get; }
}