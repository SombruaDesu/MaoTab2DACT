/*
 * @Author: MaoT
 * @Description: 射线对象
 */

using Godot;

namespace MaoTab.Scripts;

public partial class Ray : RayCast2D
{
    [Export] private Line2D _debugLine;

    public new void SetTargetPosition(Vector2 pos)
    {
        TargetPosition     = pos;
        _debugLine.Points  = [Position, pos];
    }

    public void Init()
    {
        _debugLine.Visible = false;
    }
}