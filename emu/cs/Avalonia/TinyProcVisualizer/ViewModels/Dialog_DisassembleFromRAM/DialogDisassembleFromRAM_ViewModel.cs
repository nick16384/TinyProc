namespace TinyProcVisualizer.ViewModels.Dialog_DisassembleFromRAM;

public partial class DialogDisassembleFromRAM_ViewModel : ViewModelBase
{
    public uint? DisassemblingStartAddress { get; set; } = null;
    public uint? DisassemblingEndAddress { get; set; } = null;
}