/*
 * @Author: MaoT
 * @Description: 支持异步的动画播放器
 */

using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts.Component;

/// <summary>
/// 支持异步的动画播放器
/// </summary>
[GlobalClass]
public partial class AnimationAsyncPlayer : AnimationPlayer
{
    private CancellationTokenSource _cancellationTokenSource = new();

    [Signal]
    delegate void OnAnimationFinishedEventHandler();
    
    public async void Play(string animationName, EAnimationPlayerMode loopMode)
    {
        _cancellationTokenSource.Cancel(); // 取消当前异步播放任务
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        await PlayAsync(animationName,loopMode);
        EmitSignal(SignalName.OnAnimationFinished);
    }
    
    public void PlayNotAsync(string animationName,double blend = -1D,float speed = 1f)
    {
        _cancellationTokenSource.Cancel(); // 取消当前异步播放任务
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        Play(animationName,blend,speed);
    }

    public Animation.LoopModeEnum PlayerModeConvert(EAnimationPlayerMode loopMode)
    {
        switch (loopMode)
        {
            case EAnimationPlayerMode.Null:
            case EAnimationPlayerMode.None:
                break;
            case EAnimationPlayerMode.Loop:
                return Animation.LoopModeEnum.Linear;
            case EAnimationPlayerMode.PingPong:
                return Animation.LoopModeEnum.Pingpong;
        }
        
        return Animation.LoopModeEnum.None;
    }

    /// <summary>
    ///  播放动画到结尾（仅限CG）
    /// </summary>
    /// <param name="animationName">动画名称</param>
    public void PlayToEnd(string animationName)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        
        var animation = GetAnimation(animationName);
        var length = animation.Length; // 获取动画的长度
        
        // BUG:
        // 由于 Godot 目前动画系统有个很艹的缺陷，
        // 如果动画播放器（AnimationPlayer）控制了一个序列帧动画播放器（AnimatedSprite2D），
        // 那么在动画结束时，会动画播放器也会停止序列帧的动画，
        // 导致动画结束时让角色 Idle 序列动画会卡在一针上，
        // 目前采用的解决方案是，把所有 CG 类动画全部的长度延长到 266600f 也就是600小时，让动画不会停止，
        // 这样可以解决切换场景导致打断 CG 的操作，下次玩家进入场景时，
        // 从这个长度为600小时的动画的中间部位开始播放，
        // 就能做到不暂停序列帧动画的同时呈现的是 CG 播放完后的结果
        PlaySection(animationName,length / 2,length);
    }
    
    public async Task PlayAsync(string animationName, 
        EAnimationPlayerMode loopMode = EAnimationPlayerMode.Null,double blend = -1D,float speed = 1f)
    {
        var animation = GetAnimation(animationName);
        
        if(animation == null)
        {
            return;
        }
        
        // 如果没有传入循环模式则跳过赋值，按资源内的配置处理
        if(loopMode != EAnimationPlayerMode.Null)
            animation.LoopMode = PlayerModeConvert(loopMode);
        
        // 取消当前异步播放任务
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        var token = _cancellationTokenSource.Token;
        this.Play(animationName,blend,speed);
        
        try
        {
            // 持续循环，直到动画播放完毕或异步播放被取消
            while (!token.IsCancellationRequested && 
                   IsQueuedForDeletion() && IsPlaying() && GetCurrentAnimation() == animationName)
            {
                // 等待下一帧
                await Task.Delay(10, token); // 使用取消令牌来支持任务取消
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}