namespace TinyProc.Assembling.Sections;

public interface IAssemblySection
{
    public uint Size { get; }
    public List<uint> BinaryRepresentation { get; }
}