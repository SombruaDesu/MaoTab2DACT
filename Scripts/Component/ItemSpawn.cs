using Godot;

namespace MaoTab.Scripts.Component;

/// <summary>
/// 物品刷新点
/// </summary>
[GlobalClass]
public partial class ItemSpawn : Node2D
{
    
    
    
    /// <summary>
    /// 刷新数据
    /// </summary>
    [Export] public ItemSpawnData Data;
}