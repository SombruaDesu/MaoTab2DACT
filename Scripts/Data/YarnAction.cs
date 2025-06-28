/*
 * @Author: MaoT
 * @Description: Yarn交互数据
 */


using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class YarnAction : GodotObject
{
    public string Info;

    public EDlgType Type;
    
    // 静态方法，用于创建并初始化对象
    public static YarnAction Create(string info, EDlgType type)
    {
        return new YarnAction
        {
            Info = info,
            Type = type
        };
    }
}
