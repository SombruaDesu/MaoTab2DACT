using System.Collections.Generic;
using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class RoleAgent : Node
{
    private float _oneTilePx;      // 例如 16 或 32
    
    public const float Speed = 150.0f;
    public const float JumpVelocity = -500.0f;
    public const float SmallJumpVelocity = -390.0f;
    public const float TinyJumpVelocity = -290.0f;
    
    public           float            gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private CharacterBody2D  roleBody;
    [Export] private TileMapPathFind  _pathFind2D;
    private          Stack<PointInfo> _path                       = new Stack<PointInfo>();
    private          PointInfo        _target                     = null;
    private          PointInfo        _prevTarget                 = null;
    private          float            JumpDistanceHeightThreshold = 120.0f;
    
   // private Vector2 targetPos;
    
    [Export] public int StepHeight = 1;      // 1 = 一格台阶可直接走
    
    
    private void GoToNextPointInPath()
    {
        // 如果路径为空
        if (_path.Count <= 0)
        {
            _prevTarget = null;
            _target = null;
            return;
        }

        _prevTarget = _target; // 将先前的目标设置为当前目标
        _target = _path.Pop(); // 将目标节点设置为堆栈中的下一个目标
    }
    
    private void DoPathFinding(Vector2 targetPos)
    {
        var playerTilePosition = _pathFind2D.LocalToMap(targetPos);
        _path = _pathFind2D.GetPlanform2DPath(roleBody.Position, _pathFind2D.MapToLocal(playerTilePosition));
        GoToNextPointInPath();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity  = roleBody.Velocity;
        Vector2 direction = Vector2.Zero;

        // 施加重力
        velocity.Y += gravity * (float)delta;

        // 如果存在需要追踪的目标
        if (_target != null)
        {
            // 如果目标在当前位置的右侧
            if (_target.Position.X - 5 > roleBody.Position.X)
            {
                direction.X = 1f;
            }
            // 如果目标在当前位置的左侧
            else if (_target.Position.X + 5 < roleBody.Position.X)
            {
                direction.X = -1f;
            }
            else
            {
                if (roleBody.IsOnFloor())
                {
                    GoToNextPointInPath();
                    Jump(ref velocity);
                }
            }
        }

        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(roleBody.Velocity.X, 0, Speed);
        }

        roleBody.Velocity = velocity;
        roleBody.MoveAndSlide();
    }

    private bool JumpRightEdgeToLeftEdge()
    {
        if (Mathf.Abs(_pathFind2D.LocalToMap(_prevTarget.Position).Y -
                      _pathFind2D.LocalToMap(_target.Position).Y) <= StepHeight)
            return false;         // 台阶以内不跳

        /* 原有条件保持不变 */
        return _prevTarget.IsRightEdge && _target.IsLeftEdge &&
               _prevTarget.Position.Y <= _target.Position.Y &&
               _prevTarget.Position.X < _target.Position.X;
    }

    private bool JumpLeftEdgeToRightEdge()
    {
        if (Mathf.Abs(_pathFind2D.LocalToMap(_prevTarget.Position).Y -
                      _pathFind2D.LocalToMap(_target.Position).Y) <= StepHeight)
            return false;

        return _prevTarget.IsLeftEdge && _target.IsRightEdge &&
               _prevTarget.Position.Y <= _target.Position.Y &&
               _prevTarget.Position.X > _target.Position.X;
    }

    private void Jump(ref Vector2 velocity)
    {
        if (_prevTarget == null || _target == null || _target.IsPositionPoint)
            return;

        // 计算两点在 TileMap 中的 Y 差（单位：格）
        int heightDistanceTiles = _pathFind2D.LocalToMap(_prevTarget.Position).Y -
                                  _pathFind2D.LocalToMap(_target.Position).Y;
        // ↑ 正数表示目标在上一格

        // 台阶高度以内 → 不跳
        if (Mathf.Abs(heightDistanceTiles) <= StepHeight)
            return;

        /* ---------- 以下为原有跳跃代码 ---------- */

        // 如果前一目标在上一目标之下并且距离比较近……
        if (_prevTarget.Position.Y < _target.Position.Y
            && _prevTarget.Position.DistanceTo(_target.Position) < JumpDistanceHeightThreshold)
            return;

        if (_prevTarget.Position.Y < _target.Position.Y && _target.IsFallTile)
            return;

        if (_prevTarget.Position.Y > _target.Position.Y ||
            JumpRightEdgeToLeftEdge() || JumpLeftEdgeToRightEdge())
        {
            if (Mathf.Abs(heightDistanceTiles) <= 1)
                velocity.Y = TinyJumpVelocity;
            else if (Mathf.Abs(heightDistanceTiles) == 2)
                velocity.Y = SmallJumpVelocity;
            else
                velocity.Y = JumpVelocity;
        }
    }
}