/*
 * @Author: MaoT
 * @Description: 动画播放模式
 */


namespace MaoTab.Scripts;

/// <summary>
/// 动画播放模式
/// </summary>
public enum EAnimationPlayerMode
{
    /// <summary>
    /// 空配置，用于跳过配置
    /// </summary>
    Null,
    /// <summary>
    /// 没有任何模式
    /// </summary>
    None,
    /// <summary>
    /// 循环，从头开始
    /// </summary>
    Loop,
    /// <summary>
    /// 乒乓
    /// </summary>
    PingPong,
}