using Godot;
using YarnSpinnerGodot;

namespace MaoTab.Scripts;

public partial class YarnRuntime
{
    public void BindCommand()
    {
        CommandDispatcher.AddCommandHandler("wait" , () => { });
        
        CommandDispatcher.AddCommandHandler("define", () =>
        {
            StartCaptureMode();
            Continue();
        });
        
        CommandDispatcher.AddCommandHandler("end", () =>
        {
            Game.Interface.DlgPanel.Hide(false);
            IsRunning = false;
        });
        
        CommandDispatcher.AddCommandHandler("pass", () =>
        {
            Game.Interface.DlgPanel.Hide(false);
            Continue();
        });
        
        CommandDispatcher.AddCommandHandler<string,string>("add_level_tag", (scene,tag) =>
        {
            Game.Scene.AddTag(scene,tag);
            Continue();
        });
        
        CommandDispatcher.AddFunction<string,string,bool>("has_level_tag", (scene,tag) =>
        {
            return Game.Scene.HasTag(scene,tag);
        });
        
        CommandDispatcher.AddCommandHandler("do", () =>
        {
            GD.Print("----\n开始执行位于 " + _dialogue.CurrentNode + " 节点处的脚本");
            StopCaptureMode();
            
            var code = "";
            foreach (var line in GetCaptureLines())
            {
                code += line.Text.Text + "\n";
            }

            // 新建一个空的 GDScript Resource
            var script = new GDScript();

            // 把 GDScript 源码字符串写进去
            string replaced = code.Replace("·", "");
            script.SourceCode = replaced;
            
            // 调用 Reload(true) 让 GDScript 编译器重新编译
            script.Reload(true);

            // 实例化编译好的脚本
            var init = (Node)script.New();

            init.TreeExited += () =>
            {
                GD.Print("----\n位于 " +  _dialogue.CurrentNode  + " 节点处的脚本执行完毕");
                Continue();
            };
        
            Game.Scene.AddChild(init);
        });
    }
}