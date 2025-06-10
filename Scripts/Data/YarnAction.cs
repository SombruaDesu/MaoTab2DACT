using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class YarnAction : GodotObject
{
    public string Info;

    // 静态方法，用于创建并初始化对象
    public static YarnAction Create(string info)
    {
        return new YarnAction
        {
            Info = info
        };
    }
}
