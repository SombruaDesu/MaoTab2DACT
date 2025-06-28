/*
 * @Author: MaoT
 * @Description: 玩家对象，动画部分
 */

using System;
using Godot;
using Godot.Collections;
using MaoTab.Scripts.Component;

namespace MaoTab.Scripts;

public partial class Player
{
    [Export] private AnimationAsyncPlayer _animationPlayer;
    
    [Export] private Array<AudioPlayer> _audioPlayers;
    
    public enum EAnimationState : byte
    {
        None,
        /*站立*/ Idle,
        /*挂墙*/ WallHang,
        /*起走*/ WalkStart, /*走路循环*/ Walk,    /*走路结束*/ WalkEnd,
        /*起跑*/ RunStart,  /*跑步循环*/ Run,     /*刹车*/     RunEnd,
        /*跳*/   Jump,      /*坠落*/     Falling, /*落地*/     Landing,
        /*攻击*/ Attack,
        /*受伤*/ Harm,
    }
    
    /// <summary>
    /// 动画速度
    /// </summary>
    private global::System.Collections.Generic.Dictionary<EAnimationState, float> _animationSpeed;
    
    public void PlayStepAudio()
    {
        var id = Random.Shared.Next(0, _audioPlayers.Count);
        _audioPlayers[id].Play();
    }
    
    /// <summary>
    /// 初始化动画
    /// </summary>
    private void InitAnimation()
    {
        _animationSpeed = new global::System.Collections.Generic.Dictionary<EAnimationState, float>
        {
            { EAnimationState.None, 0f },
            { EAnimationState.Idle, 1f },
            { EAnimationState.Run, 1f },
            { EAnimationState.Walk, 1f },
            { EAnimationState.Jump, 1f },
            { EAnimationState.Falling, 1f },
            { EAnimationState.Landing, 1f },
            { EAnimationState.WallHang, 1f },
            { EAnimationState.Attack, 1f }
        };
    }
    
    private async void PlayAnimation()
    {
        
        switch (Data.State)
        {
            case EAnimationState.Idle:
                _animationPlayer.PlayNotAsync("Idle");
                break;
            case EAnimationState.Walk:
                _animationPlayer.PlayNotAsync("Walk",-1D, _animationSpeed[EAnimationState.Walk]);
                break;
            case EAnimationState.Run:
                _animationPlayer.PlayNotAsync("Run", -1D, _animationSpeed[EAnimationState.Run]);
                break;
            case EAnimationState.Jump:
                _animationPlayer.PlayNotAsync("Jump");
                break;
            case EAnimationState.Falling:
                _animationPlayer.PlayNotAsync("Falling");
                break;
            case EAnimationState.WallHang:
                _animationPlayer.PlayNotAsync("WallHang");
                break;
            case EAnimationState.Attack:
                await _animationPlayer.PlayAsync("Attack");
                AttackOver();
                break;
            case EAnimationState.Harm:
                await _animationPlayer.PlayAsync("Harm");
                break;
            case EAnimationState.None:
            default: break;
        }
    }
    
    /// <summary>
    /// 动画更新逻辑，根据角色的当前状态选择播放的动画
    /// </summary>
    private void Animation()
    {
        EAnimationState newState = EAnimationState.None;

        if (_isAttack)
        {
            newState = EAnimationState.Attack;
        }
        else if(_isHarm)
        {
            newState = EAnimationState.Harm;
        }
        else if (_isWallHanging)
        {
            newState = EAnimationState.WallHang;
        }
        else
        {
            // 如果不在地面上，则默认为跳跃状态（可扩展为上升和下降分开处理）
            if (!IsOnFloor())
            {
                if (_targetVelocity.Y < 0)
                {
                    newState = EAnimationState.Jump;
                }
                else if(_targetVelocity.Y > 0)
                {
                    newState = EAnimationState.Falling;
                }
            }
            else
            {
                // 在地面上时，判断玩家是否有输入水平移动
                if (Math.Abs(InputMoveDirection.X) > 0.1f)
                {
                    if (Run)
                    {
                        newState = EAnimationState.Run;
                    }
                    else
                    {
                        newState = EAnimationState.Walk;
                    }
                }
                else
                    newState = EAnimationState.Idle;
            }
        }
        
        // 状态切换时播放相应动画
        if (newState != Data.State)
        {
            Data.State = newState;
            PlayAnimation();
        }
    }
}