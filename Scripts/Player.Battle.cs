/*
 * @Author: MaoT
 * @Description: 玩家对象，战斗部分
 */

using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    private       bool  _isAttack;                     // 是否正在攻击
    private       float _attackCooldownTimer;          // 冷却计时器
    private const float AttackCooldownDuration = 0.1f; // 攻击冷却时间（秒）

    private bool _isHarm;
    private bool _isDead;
    
    
    public void HarmFromPoint(Vector2  point,Vector2 power)
    {
        _isHarm               = true;
        _targetVelocity = Vector2.Zero; 
        
        _sprite.Modulate = new Color(Colors.Red);
        
        var imp     = point.LookAt(Position);
        Vector2 rePower;
        if (imp.X > 0)
        {
            SetFacing(true);
            rePower = new Vector2(power.X, -power.Y);
        }
        else
        {
            SetFacing(false);
            rePower = new Vector2(power.X * -1, -power.Y);
        }
        
        ApplyImpulse(rePower);
        if(_items.Count > 0)
            DropItem(_items.Last(), new Vector2(rePower.X,rePower.Y * 5));
        
        OnExternalImpulseDissipate += () =>
        {
            _sprite.Modulate = new Color(Colors.White);
            _isHarm          = false;
        };
    }
    
    public void AttackInput()
    {
        // 如果正在攻击或冷却中，不能发起新的攻击
        if (_isAttack || _attackCooldownTimer > 0) return;

        _isAttack    = true;
        Data.Movable = false;

        // 在这里可以触发攻击动画或逻辑
    }

    private void AttackOver()
    {
        if (!_isAttack) return;

        _isAttack    = false;
        Data.Movable = true;

        // 开始攻击冷却计时
        _attackCooldownTimer = AttackCooldownDuration;
    }
 
    public void Kill()
    {
        Dead();
    }
    
    public async Task Dead()
    {
        _isDead         = true;
        _targetVelocity = Vector2.Zero; 
        
        await Game.Interface.LoadStart();
        Position = Game.Scene.GetLastSafePoint();
        Game.Interface.LoadOver();
        _isDead = false;
    }
    
    public void UpdateBattleSystem()
    {
        if (_isAttack)
        {
            
        }
        else
        {
            // 如果没有正在攻击，处理冷却计时器
            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= (float)Game.PhysicsDelta; // 每帧减少冷却时间
                if (_attackCooldownTimer <= 0)
                {
                    _attackCooldownTimer = 0; // 防止计时器变成负值
                    // 冷却完成，可以再次攻击
                }
            }
        }
    }

}