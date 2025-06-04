/*
 * @Author: MaoT
 * @Description: 游戏管理器
 */

using System.Collections.Generic;

namespace MaoTab.Scripts;

public static partial class Game
{
    public static double PhysicsDelta;
    
    /// <summary>
    /// 从Root初始化完毕时起，游戏循环次数
    /// </summary>
    public static int Loop;
    
    /// <summary>
    /// 当前客户端控制的角色
    /// </summary>
    public static Player MainPlayer;
    
    public static Player OtherPlayer;
    
    public static HashSet<string> Tags = new();
    
    public static void Tick(double delta)
    {
        PhysicsDelta = delta;
        Loop++;
    }
}