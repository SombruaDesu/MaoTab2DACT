/*
 * @Author: MaoT
 * @Description: 玩家对象，战斗部分
 */

namespace MaoTab.Scripts;

public partial class Player
{
    private       bool  _isAttack;                     // 是否正在攻击
    private       float _attackCooldownTimer;          // 冷却计时器
    private const float AttackCooldownDuration = 0.1f; // 攻击冷却时间（秒）

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