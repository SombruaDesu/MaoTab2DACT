using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class InteractionAction : GodotObject
{
    public InteractionType Type;
    public string          Info;

    // 静态方法，用于创建并初始化对象
    public static InteractionAction Create(InteractionType type, string info)
    {
        return new InteractionAction
        {
            Type = type,
            Info = info
        };
    }
}

public enum InteractionType
{
    Yarn
}