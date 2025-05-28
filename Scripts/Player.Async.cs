/*
 * @Author: MaoT
 * @Description: 玩家对象，网络同步部分
 */

using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    /// <summary>
    /// 同步平滑因子
    /// </summary>
    private const float AsyncSmoothFactor = 0.5f;
    
    public void Async(PlayerData newData)
    {
        // 同步角色的数据
        Data.Position = newData.Position;
        Data.State  = newData.State;
        Data.Facing = newData.Facing;
        
        _sprite.FlipH = Data.Facing;
        PlayAnimation();
    }

    public void AsyncTick()
    {
        Position =  Position.Lerp(Data.Position, AsyncSmoothFactor);
    }
}