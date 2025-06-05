/*
 * @Author: MaoT
 * @Description: 摄像机对象
 */

using System;
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

    private float   decay     = 0.8f;
    private Vector2 maxOffset = new(100,75);
    private float   maxRoll   = 0.1f;
    private float   trauma;
    private int     traumaPower = 2;
    
    Random rand = new Random();
    
    private void Shake()
    {
        var    amout       = Mathf.Pow(trauma,traumaPower);
        Rotation = maxRoll * amout * (rand.NextSingle() * 2 - 1);
        Offset = new Vector2(
            maxOffset.X * amout * (rand.NextSingle() * 2 - 1),
            maxOffset.Y * amout * (rand.NextSingle() * 2 - 1));
        
    }

    public void AddTrauma(float amount)
    {
        trauma = MathF.Min(trauma + amount,1);
    }
    
    public void Tick()
    {
        if (trauma != 0)
        {
            trauma = Mathf.Max(trauma - decay * (float)Game.PhysicsDelta,0);
            Shake();
        }
        
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