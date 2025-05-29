using Godot;
using MaoTab.Scripts.Component;
using MaoTab.Scripts.System;

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
        
        CommandDispatcher.AddCommandHandler("do", () =>
        {
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
                GD.Print("脚本执行完毕");
                Continue();
            };
        
            Game.Scene.AddChild(init);
        });
    }
}