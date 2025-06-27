/*
 * @Author: MaoT
 * @Description: 物品刷新点数据
 */


using Godot;
using Godot.Collections;

namespace MaoTab.Scripts;

/// <summary>
/// 物品刷新日志
/// </summary> 
[GlobalClass]
public partial class ItemSpawnData : Resource
{
    /// <summary>
    /// 可刷新的物品ID
    /// </summary>
    [Export] public Array<string> IdList;

    /// <summary>
    /// 上一个刷新的ID
    /// </summary>
    public string PreId;

    /// <summary>
    /// 刷新概率
    /// </summary>
    [Export] public float SpawnProbability;
}