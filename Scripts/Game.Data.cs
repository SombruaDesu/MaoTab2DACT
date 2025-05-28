/*
 * @Author: MaoT
 * @Description: 游戏管理器，数据部分
 */

using Godot;
using LiteNetLib.Utils;

namespace MaoTab.Scripts;

public static partial class Game
{
    public static void Save()
    {
        var playerData = new NetDataWriter();
        
        MainPlayer.Data.Serialize(playerData);
        
        
        
        
    }
}