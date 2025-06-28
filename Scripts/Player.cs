﻿/*
 * @Author: MaoT
 * @Description: 玩家对象，核心移动部分
 */

using System;
using Godot;

namespace MaoTab.Scripts;

/// <summary>
/// 玩家对象
/// </summary>
[GlobalClass]
public partial class Player : CharacterBody2D
{
    public int Id;
    
    // 玩家输入：仅取 X 分量指示左右移动；Y 分量（大于 0）用于跳跃
    public Vector2 InputMoveDirection = Vector2.Zero;

    /// <summary>
    /// 体力
    /// </summary>
    public float PS;
    
    /// <summary>
    /// 奔跑状态（默认奔跑）
    /// </summary>
    public bool Run = true;

    public PlayerData Data = new()
    {
        Movable          = true,       // 是否允许移动
        WalkSpeed        = 50.0f,      // 行走速度（像素/秒）
        RunSpeed         = 50f * 2.2f, // 奔跑速度（像素/秒）
        JumpImpulse      = 400,        // 跳跃冲量（向上为负）
        FallAcceleration = 1000,       // 重力加速度（像素/秒²）
        State            = EAnimationState.Idle
    };

    [Export] private AnimatedSprite2D _sprite; // 精灵渲染对象
    
    // 头部水平射线
    [Export] private Ray _headHRay;

    // 脚本水平射线
    [Export] private Ray _footHRay;

    // 当前速度（使用 CharacterBody2D 提供的 Velocity）
    private Vector2 _targetVelocity = Vector2.Zero;

    // 外力相关
    private Vector2 _externalImpulse        = Vector2.Zero; // 瞬时外力（一次性施加，例如后坐力）
    private Vector2 _externalForce          = Vector2.Zero; // 持续施加，不自动清除
    private Vector2 _pendingExternalImpulse = Vector2.Zero;

    /// <summary>
    /// 当一次性冲力消散时
    /// </summary>
    private Action OnExternalImpulseDissipate;
    
    // 摩擦力属性：
    // 地面摩擦力，值越大表示当前角色所站立的地面越“粗糙”，实际摩擦效果越强，
    // 但同时受到角色 Weight 的影响：角色越重，越容易站稳（外力影响衰减越大），默认 100
    public int GroundFriction = 80;

    // 空中的摩擦力，通常在空中摩擦效果较小，默认 0（完全平滑）
    public int AirFriction = 0;

    // 重量，数字越大，角色下降加速度越快，同时作为地面抗向上外力的门槛
    public int Weight = 100;

    // 跳跃水平冲量比例（相对于有效跳跃冲量）
    // 例如当有效跳跃冲量为 400 时，则左右额外补充 80（400×0.2）的水平冲量
    public float JumpDirectionalBoostFactor = 0.2f;

    /// <summary>
    /// 有效跳跃冲量，结果角色体重等因素得出的冲量
    /// </summary>
    private float EffectiveJumpImpulse
    {
        get
        {
            // 体重 100 就是标准，低于这个体重也不会增加跳跃的高度
            return Data.JumpImpulse * Mathf.Min(100f / Weight, 100);
        }
    }

    // 跳跃相关状态
    private bool _jumpRequested;          // 跳跃请求标志
    private bool _jumpButtonHeld;         // 跳跃长按标志
    private bool _jumpKeyReleased = true; // 跳跃输入松开标志，用于判断是否已经松开跳跃键
    private int  _jumpCount;              // 跳跃计数器

    private float _jumpTime; // 用于记录跳跃持续时间

    private float _currentJumpImpulse; // 记录当前跳跃的向上冲量

    /// <summary>
    /// 狼跳计时器
    /// </summary>
    /// <para>为零时代表失效，随时间减少</para>
    private float _coyoteTimer;

    private bool _wallHangRequested; //  挂墙请求标志
    private bool _isWallHanging;     // 是否挂墙

    private float JumpHeldIncrement => Data.FallAcceleration * 0.75f; // 长按跳跃时使用的重力因子

    public bool AllowJumpWhileFalling = true; // 是否仅在下坠时允许连跳

    /// <summary>
    /// 最大跳跃次数
    /// </summary>
    public int MaxJumpCount = 1;

    // 常量因子，用于计算动态长跳时间
    // 当 Weight 为 100 且 JumpImpulse 为 400 时，dynamicJumpTime 约为 0.5 秒
    private const float BaseJumpTimeFactor = 0.125f;

    /// <summary>
    /// 最大狼跳有效时间
    /// </summary>
    public float CoyoteTime = 0.2f;

    // 初始化标记
    private bool _initialized;

    /// <summary>
    /// 初始化玩家状态（游戏本地生成时调用，仅会调用一次类似 _Ready ）
    /// </summary>
    public void Init(bool isEntity)
    {
        Data.Player = this;

        if (!isEntity)
        {
            CollisionLayer = 0;
        }
        
        _targetVelocity = Vector2.Zero;
        _jumpRequested  = false;
        _coyoteTimer    = CoyoteTime;
        _initialized    = true;
        
        _footHRay.Init();
        _headHRay.Init();
        
        InitBackpack(3,5);
        
        SetFacing(Data.Facing);
        InitAnimation();
    }
    
    public void Pause()
    {
        
    }

    private void ResetInput()
    {
        InputMoveDirection = Vector2.Zero;
        _wallHangRequested = false;
        _jumpRequested     = false;
        _jumpKeyReleased   = true;
        _jumpButtonHeld    = false;
        _jumpTime          = 0;
    }
    
    /// <summary>
    /// 每帧调用，类似 _PhysicsProcess()
    /// </summary>
    public void Tick()
    {
        if (!_initialized)
            return;
        
        float dt = (float)Game.PhysicsDelta; // 缓存每帧物理时间增量
        
        UpdateBattleSystem();
        
        // 重置 _targetVelocity 为玩家输入决定的水平速度， 后续再叠加垂直与外力影响
        _targetVelocity.X = InputMoveDirection.X * (Run ? Data.RunSpeed : Data.WalkSpeed);
        
        // ────────── 处理挂墙操作 ──────────
        WallHang();
        
        // ────────── 处理跳跃、重力 ──────────
        UpdateVertical(dt);

        // ────────── 处理角色翻转 ──────────
        Flip();

        // ────────── 添加瞬时外力效果 ──────────
        if (_isHarm)
        {
            // ------- 复杂效果处理 -------
            // 当需要冲力在x，y轴几乎同时完成时使用复杂效果
            // 如果不需要平滑且大幅度的冲量时用下面的处理效果
            const float SX                = 0.2f; // 横向固定平滑系数
            const float MIN_LIFT_VELOCITY = 60f;  // 最低竖直速度

            // ------- 计算本帧应该使用的 sy（越大越“立刻”） -------
            float absImpY = Mathf.Abs(_pendingExternalImpulse.Y);

            // 如果没有竖直冲量或角色已离地，就跟 X 一样平滑
            float sy;
            if (absImpY < 0.01f)
            {
                sy = SX;
            }
            else
            {
                // 期望第一帧就能产生的竖直速度占比
                // ratio = MIN_LIFT_VELOCITY / absImpY
                // 再保证 sy ∈ [SX, 1]
                sy = Mathf.Clamp(MIN_LIFT_VELOCITY / absImpY, SX, 1f);
            }

            // ------- 按两个不同系数把冲量加入 _externalImpulse -------
            _externalImpulse.X += (_pendingExternalImpulse.X - _externalImpulse.X) * SX;
            _externalImpulse.Y += (_pendingExternalImpulse.Y - _externalImpulse.Y) * sy;

            // 叠加到目标速度（立即生效）
            _targetVelocity += _externalImpulse;

            // ------- 把尚未消耗完的冲量继续衰减 -------
            _pendingExternalImpulse.X = Mathf.Lerp(_pendingExternalImpulse.X, 0, SX);
            _pendingExternalImpulse.Y = Mathf.Lerp(_pendingExternalImpulse.Y, 0, sy);
        
            // 当一次性冲力衰竭到一定程度时，视为完成
            if (_pendingExternalImpulse.AetV2(Vector2.Zero, 1f))
            {
                _externalImpulse        = Vector2.Zero;
                _pendingExternalImpulse = Vector2.Zero;
                
                OnExternalImpulseDissipate?.Invoke();
                OnExternalImpulseDissipate = null;
            }
        }
        else
        {
            // ------- 简单效果处理 ------- 
            // 将待施加的外力平滑过渡到 _externalImpulse，这里可以调节 smoothingFactor 控制插值速度
            float smoothingFactor = 0.2f;  // 数值越小，外力施加越慢
            _externalImpulse += (_pendingExternalImpulse - _externalImpulse) * smoothingFactor;
        
            _targetVelocity += _externalImpulse;
            _externalImpulse =  Vector2.Zero; // 施加后清零

            // 每帧衰减待施加外力（避免累积过多）
            _pendingExternalImpulse = _pendingExternalImpulse.Lerp(Vector2.Zero, smoothingFactor);
        }
        
        // ────────── 添加持续外力（例如风力） ──────────
        float multiplier;
        if (IsOnFloor())
        {
            // 当在地面时，外力的有效影响按 GroundFriction 与 Weight 衰减：
            // 当 GroundFriction 为 0，表示平滑地面，则 multiplier 为 1
            // 当 GroundFriction 增大， multiplier 按公式降低
            multiplier = Mathf.Clamp(1.0f - (float)GroundFriction * Weight / 10000f, 0f, 1f);
        }
        else
        {
            // 空中时，使用公式： multiplier = 1 - (AirFriction / 100)
            multiplier = 1.0f - AirFriction / 100f;
        }

        // 结算暴雨冲击力
        if (Game.WeatherStrength / 100f >= 0.8f)
        {
            Data.JumpImpulse = 400 / 2;
        }
        else
        {
            Data.JumpImpulse = 400;
        }
        
        _targetVelocity += _externalForce * dt * multiplier;
        
        // 在地面时，如果外力产生的向上（负 Y）速度不足以克服 Weight，则不使角色离地
        if (!_isHarm && IsOnFloor())
        {
            if (_targetVelocity.Y < 0 && Mathf.Abs(_targetVelocity.Y) < Weight)
            {
                _targetVelocity.Y = 0;
            }
        }
        
        // 执行移动
        Velocity = _targetVelocity;
        MoveAndSlide();

        // 检测天花板碰撞，取消向上冲力
        HandleCeilingCollision();

        // 更新特效
        FxUpdate();
        
        // 更新动画状态（方法内部实现动画切换逻辑）
        Animation();

        // 缓存玩家的位置
        Data.Position = Position;
    }

    /// <summary>
    /// 切换角色朝向（根据水平方向输入）
    /// </summary>
    private void Flip()
    {
        // 如果正处于挂墙状态，不处理翻转逻辑
        if (_isWallHanging)
            return;

        // 没有移动输入时也不处理
        if (InputMoveDirection.X == 0)
            return;

        bool newDir = !(InputMoveDirection.X > 0);
        if (newDir == Data.Facing)
            return;

        SetFacing(newDir);
    }

    private void SetFacing(bool facing)
    {
        // 根据输入调整角色精灵和射线朝向
        if (facing)
        {
            _sprite.FlipH = true;
            _footHRay.SetTargetPosition(new Vector2(-10.2f, 0));
            _headHRay.SetTargetPosition(new Vector2(-10.2f, 0));
            _startTile.Scale = new Vector2(-1f, 1f);
        }
        else
        {
            _sprite.FlipH = false;
            _footHRay.SetTargetPosition(new Vector2(10.2f, 0));
            _headHRay.SetTargetPosition(new Vector2(10.2f, 0));
            _startTile.Scale = new Vector2(1f, 1f);
        }

        Data.Facing = facing;
    }

    /// <summary>
    /// 挂墙检测
    /// </summary>
    private void WallHang()
    {
        // 挂墙时的逻辑
        if(_isWallHanging)
        {
            // 挂墙时禁止左右移动
            _targetVelocity.X = 0;
            
            // 如果没有挂墙输入则取消挂墙
            if (!_wallHangRequested)
            {
                _isWallHanging = false;
            }
            return;
        }
        
        // 你可能会问为什么上面的那段不直接写在下面的判断里（当然它现在可能已经不是判断块，但是未来肯定会有）
        // 1. 它能减少不必要的碰撞检测
        // 2. 避免高频物理判断带来的几帧的状态变化，这样会使人物动画鬼畜
        
        // 当角色头部和脚部射线均检测到墙体，并且玩家按住挂墙键时，则进入挂墙状态
        _isWallHanging = _headHRay.IsColliding() && _footHRay.IsColliding() && _wallHangRequested;
    }

    /// <summary>
    /// 着落，重置玩家的跳跃相关数值
    /// </summary>
    private void Landing()
    {
        _coyoteTimer      = CoyoteTime;
        _jumpTime         = 0;
        _jumpCount        = 0; // 重置跳跃次数
        _targetVelocity.Y = 0; // 重置垂直速度
    }

    /// <summary>
    /// 处理垂直方向上的跳跃与重力逻辑
    /// </summary>
    private void UpdateVertical(float dt)
    {
        // ────────── 处理非常规重力 ──────────
        // 挂墙时冻结垂直移动，并且不处理重力
        if (_isWallHanging)
        {
            // 如果同时按下跳跃键，则执行墙跳
            if (_jumpRequested)
            {
                // 退出挂墙状态
                _isWallHanging = false;

                // 执行跳跃：设置垂直速度
                _pendingExternalImpulse.Y = -EffectiveJumpImpulse * 0.75f;

                // 这里利用玩家原本的水平输入方向来决定反弹方向
                if (Data.Facing)
                {
                    _pendingExternalImpulse.X += EffectiveJumpImpulse * 3;
                }
                else
                {
                    _pendingExternalImpulse.X += -EffectiveJumpImpulse * 3;
                }

                SetFacing(!Data.Facing);

                _jumpRequested = false;
                _jumpCount     = 1;

                return;
            }

            _targetVelocity.Y = 0;
            return;
        }

        // ────────── 处理常规重力、跳跃逻辑 ──────────
        // 若在地面，则重置狼跳计时器和跳跃时间
        if (IsOnFloor())
        {
            Landing();
        }
        else
        {
            _coyoteTimer = Mathf.Max(_coyoteTimer - dt, 0);
        }

        // 计算动态长跳有效时间
        float dynamicJumpTime = BaseJumpTimeFactor * Data.JumpImpulse / Weight;

        // 处理跳跃请求（包括初始跳跃和空中跳跃）
        if (_jumpRequested)
        {
            // 根据角色重量调节跳跃冲量
            bool hasJumped = false;
            // 记录跳跃冲力（绝对值）
            _currentJumpImpulse = EffectiveJumpImpulse;
            
            // 起跳：在地面或者在狼跳时间内
            if (IsOnFloor() || _coyoteTimer > 0)
            {
                _targetVelocity.Y = -EffectiveJumpImpulse;
                _jumpCount        = 1;
                _coyoteTimer      = 0; // 使用后失效
                hasJumped         = true;
            }
            // 多段跳：不在地面，并且 MaxJumpCount>=2 时才允许空中进行多段跳跃
            else if (MaxJumpCount > 1 && _jumpCount < MaxJumpCount && AllowJumpWhileFalling && Velocity.Y > 0)
            {
                _targetVelocity.Y = -EffectiveJumpImpulse;
                _jumpCount++;
                hasJumped = true;
            }

            // 若跳跃时有左右输入，则额外补充水平冲力
            if (hasJumped && InputMoveDirection.X != 0)
            {
                float horizontalBoost = EffectiveJumpImpulse * JumpDirectionalBoostFactor;
                _targetVelocity.X += Mathf.Sign(InputMoveDirection.X) * horizontalBoost;
            }

            // 清除跳跃请求和重置跳跃持续时间
            _jumpRequested = false;
            _jumpTime      = 0;
        }
        // 处理长按跳跃，以及下坠
        else
        {
            if (!IsOnFloor())
            {
                // 当按住跳跃时，延长跳跃时间（未使用重量影响）
                if (_jumpButtonHeld && _jumpTime < dynamicJumpTime)
                {
                    _jumpTime         += dt;
                    _targetVelocity.Y += JumpHeldIncrement * dt;
                }
                else
                {
                    // 空中下降时，下降加速度乘以重量，使得重量越大下降越快
                    _targetVelocity.Y += Data.FallAcceleration * (Weight / 100f) * dt;
                    _jumpButtonHeld   =  false;
                }
            }
        }
    }

    /// <summary>
    /// 检测头部与天花板碰撞，取消上冲速度
    /// </summary>
    private void HandleCeilingCollision()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetNormal().Dot(Vector2.Down) > 0.9f && _targetVelocity.Y < 0)
            {
                _targetVelocity.Y   = Mathf.Max(_targetVelocity.Y + _currentJumpImpulse, 0);
                _currentJumpImpulse = 0;
                break;
            }
        }
    }

    /// <summary>
    /// 玩家输入接口（每帧调用，由 Root 传入当前输入）
    /// </summary>
    /// <param name="direction"> direction.X 控制左右移动，direction.Y 大于 0 表示按下跳跃键</param>
    /// <param name="isRun">isRun 为 true 时表示奔跑（由于默认为奔跑，所以按下时为步行）</param>
    /// <param name="isWallHang">isWallHang 为 true 时表示爬墙</param>
    public void Input(Vector2 direction, bool isRun, bool isWallHang)
    {
        if (!Data.Movable) return;

        if (_isHarm)
        {
            ResetInput();
            return;
        }
        
        // 只使用 X 分量处理左右移动
        InputMoveDirection = new Vector2(direction.X, 0);

        _wallHangRequested = isWallHang;

        // 跳跃按键处理：只有在键已松开后才触发新跳跃
        if (direction.Y > 0 && MaxJumpCount > 0)
        {
            if (_jumpKeyReleased)
            {
                // 只有在...
                if (IsOnFloor() || // 地板上
                    _jumpCount < MaxJumpCount || // 或 还有多段跳跃的次数时
                    _coyoteTimer > 0 || // 或 狼跳生效时间内
                    _isWallHanging) // 或 挂墙时
                    _jumpRequested = true; // 才处理跳跃输入
                
                _jumpKeyReleased = false;
            }

            _jumpButtonHeld = true;
        }
        else
        {
            _jumpKeyReleased = true;
            _jumpButtonHeld  = false;
        }

        // 若按下奔跑键则切换为步行，反之为奔跑
        Run = !isRun;
    }

    /// <summary>
    /// 施加瞬间的外力（例如后坐力）。
    /// 该外力将在下一帧 Tick 时被添加并立即清除。
    /// </summary>
    /// <param name="impulse">施加的外力向量</param>
    public void ApplyImpulse(Vector2 impulse)
    {
        _pendingExternalImpulse += impulse;
    }

    /// <summary>
    /// 设置/增加持续外力（例如风力）。
    /// 注意：持续外力不会自动清除，需要手动归零。
    /// </summary>
    /// <param name="force">施加的持续外力向量</param>
    public void AddExternalForce(Vector2 force)
    {
        _externalForce += force;
    }

    /// <summary>
    /// 清除所有持续外力的影响（例如当离开风区时调用）。
    /// </summary>
    public void ClearExternalForce()
    {
        _externalForce = Vector2.Zero;
    }
}