/*
 * @Author: MaoT
 * @Description: 游戏状态节点，用于与 GDS 交互游戏状态，由引擎Autoload生成
 */


using Godot;

namespace MaoTab.Scripts.Component;

/// <summary>
/// 游戏状态节点，用于与 GDS 交互游戏状态，由引擎Autoload生成
/// </summary>
[GlobalClass]
public partial class GameState : Node
{
    public static int GetGameLoop() { return Game.Loop; }
    public static Player GetPlayer() { return Game.MainPlayer; }
}