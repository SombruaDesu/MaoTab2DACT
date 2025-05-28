/*
 * @Author: MaoT
 * @Description: 摄像机对象
 */

using Godot;

namespace MaoTab.Scripts.Component;

public partial class Camera : Camera2D
{
    // 摄像机跟踪对象
    public Node2D FollowTarget;
    
    // 用于控制基础跟踪速度
    private const float BASE_SPEED = 0.25f;
    
    // 为避免当距离太小时速度过小，设置最小影响因子
    private const float MIN_FACTOR = 1f;

    public void Tick()
    {
        if (FollowTarget == null)
        {
            return;
        }
        
        // 计算摄像机当前位置和目标之间的距离
        float distance = Position.DistanceTo(FollowTarget.Position);
        
        // 根据距离调整 lerp 的比例因子
        // 当目标较远时，跟踪速度增大；当目标较近时，保持最小的速度
        float factor = BASE_SPEED * Mathf.Max(distance, MIN_FACTOR) * (float)Game.PhysicsDelta;
        
        // 防止因距离很远导致 lerp 参数超过 1（瞬间到达），可以将该因子限制在 [0,1]
        factor = Mathf.Clamp(factor, 0f, 1f);
        
        Position = new Vector2(
            Mathf.Lerp(Position.X, FollowTarget.Position.X, factor),
            Mathf.Lerp(Position.Y, FollowTarget.Position.Y, factor));
    }
}