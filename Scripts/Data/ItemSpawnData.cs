using Godot;

namespace MaoTab.Scripts;

/// <summary>
/// 物品刷新日志
/// </summary> 
[GlobalClass]
public partial class ItemSpawnData : Resource
{
    /// <summary>
    /// 可刷新的物品名
    /// </summary>
    [Export] public string IdList;

    /// <summary>
    /// 上一个刷新的ID
    /// </summary>
    [Export] public string PreId;

    /// <summary>
    /// 刷新概率
    /// </summary>
    [Export] public float SpawnProbability;
}