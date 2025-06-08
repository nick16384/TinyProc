namespace TinyProcVisualizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ulong CurrentCPUCycle
    {
        get
        {
            ulong? currentCPUCycle = TinyProc.Application.ExecutionContainer.INSTANCE0.CurrentCycle;
            if (currentCPUCycle == null)
                return 0;
            else
                return currentCPUCycle.Value;
        }
    }
}
