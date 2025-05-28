/*
 * @Author: MaoT
 * @Description: 数学库
 */

using System;
using Godot;

namespace MaoTab.Scripts;

public static class MathfHelper
{
    public static Vector2 V2Lerp(Vector2 a, Vector2 b, float t)
    {
        // 限制 t 在 0 到 1 之间
        t = Math.Clamp(t, 0f, 1f);
        return new Vector2(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t
        );
    }
    
    /// <summary>
    /// 求近值
    /// </summary>
    /// <param name="a">值A</param>
    /// <param name="b">值B</param>
    /// <param name="tolerance">差量</param>
    /// <returns>a 近等于 b 时true</returns>
    public static bool AetF(float a, float b, float tolerance = 0.1f)
    {
        return Math.Abs(a - b) < tolerance;
    }
    
    /// <summary>
    /// 从一个 Vector2 插值到另一个 Vector2，并通过一个 0 到 1 的浮点数控制进度.
    /// 当 progress 为 0 时返回 from，当 progress 为 1 时返回 to.
    /// </summary>
    /// <param name="from">起始 Vector2</param>
    /// <param name="to">目标 Vector2</param>
    /// <param name="progress">插值进度 (0 到 1)</param>
    /// <returns>插值后的 Vector2</returns>
    public static Vector2 V2Interpolate(Vector2 from, Vector2 to, float progress)
    {
        // 限制 progress 在 0 到 1 之间
        progress = Math.Clamp(progress, 0f, 1f);
        return new Vector2(
            from.X + (to.X - from.X) * progress,
            from.Y + (to.Y - from.Y) * progress
        );
    }
}