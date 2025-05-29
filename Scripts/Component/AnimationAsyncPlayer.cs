/*
 * @Author: MaoT
 * @Description: 支持异步的动画播放器
 */

using System;
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
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    [Signal]
    delegate void OnAnimationFinishedEventHandler();
    
    public async void Play(string animationName, EAnimationPlayerMode loopMode)
    {
        // 取消当前异步播放任务
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        await PlayAsync(animationName,loopMode);
        EmitSignal(SignalName.OnAnimationFinished);
        GD.Print($"播放动画 '{animationName}' 。");
    }
    
    public void PlayNotAsync(string animationName,double blend = -1D,float speed = 1f)
    {
        // 取消当前异步播放任务
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        Play(animationName,blend,speed);
        GD.Print($"播放动画 '{animationName}' 。");
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
    
    public async Task PlayAsync(string animationName, EAnimationPlayerMode loopMode = EAnimationPlayerMode.Null,double blend = -1D,float speed = 1f)
    {
        var animation = GetAnimation(animationName);
        
        if(animation == null)
        {
            GD.Print("不存在动画" + animationName);
            return;
        }
        
        // 如果没有传入循环模式则跳过赋值，按资源内的配置处理
        if(loopMode != EAnimationPlayerMode.Null)
            animation.LoopMode = PlayerModeConvert(loopMode);
        
        // 取消当前异步播放任务
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // 重置取消令牌
        var token = _cancellationTokenSource.Token;
        Play(animationName,blend,speed);
        
        GD.Print($"播放动画 '{animationName}' 。");
        try
        {
            // 持续循环，直到动画播放完毕或异步播放被取消
            while (!token.IsCancellationRequested && IsPlaying() && GetCurrentAnimation() == animationName)
            {
                // 等待下一帧
                await Task.Delay(10, token); // 使用取消令牌来支持任务取消
            }

            if (token.IsCancellationRequested)
            {
                GD.Print($"异步动画 '{animationName}' 被取消。");
            }
            else
            {
                GD.Print($"动画 '{animationName}' 已完成。");
            }
        }
        catch (TaskCanceledException)
        {
            GD.Print($"异步动画 '{animationName}' 被取消。");
        }
    }
}