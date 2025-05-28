namespace MaoTab.Scripts;

public partial class YarnRuntime
{
    public void BindCommand()
    {
        CommandDispatcher.AddCommandHandler("wait" , () => { });
        CommandDispatcher.AddCommandHandler("define" , () =>
        {
            _scriptDefine = true;
            Continue();
        });
    }
}