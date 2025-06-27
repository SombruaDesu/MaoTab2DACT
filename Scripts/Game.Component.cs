/*
 * @Author: MaoT
 * @Description: 游戏管理器，组件托管部分
 */

using MaoTab.Scripts.Component;
using MaoTab.Scripts.Panel;

namespace MaoTab.Scripts;

public static partial class Game
{
    public static Camera Camera;
    
    /// <inheritdoc cref="MaoTab.Scripts.Scene"/>
    public static Scene Scene;
    
    /// <inheritdoc cref="MaoTab.Scripts.Interface"/>
    public static Interface Interface;
    
    /// <inheritdoc cref="MaoTab.Scripts.YarnRuntime"/>
    public static YarnRuntime Yarn;
    
    public static WeatherMgr  WeatherMgr;
}