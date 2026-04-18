using CommunityToolkit.Mvvm.Messaging.Messages;
using TinyProcVisualizer.ViewModels.Dialog_DisassembleFromRAM;

namespace TinyProcVisualizer.Messages;

public class DisassembleFromRAMMessage : AsyncRequestMessage<DialogDisassembleFromRAM_ViewModel?>;