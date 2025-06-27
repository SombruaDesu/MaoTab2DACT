/*
 * @Author: MaoT
 * @Description: 游戏管理器，数据部分
 */

using Godot;
using LiteNetLib.Utils;

namespace MaoTab.Scripts;

public static partial class Game
{
    /// <summary>
    /// 天气强度
    /// </summary>
    public static float WeatherStrength = 100;
    public static float WeatherSpeed = 100;
    
    public static void Save()
    {
        var playerData = new NetDataWriter();
        
        MainPlayer.Data.Serialize(playerData);
    }
}