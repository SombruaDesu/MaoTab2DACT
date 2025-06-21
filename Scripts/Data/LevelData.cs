using System.Collections.Generic;

namespace MaoTab.Scripts;

public struct LevelData
{
    /// <summary>
    /// 标签哈希表
    /// </summary>
    public HashSet<string> Tags;

    public string Name;

    /// <summary>
    /// 物品刷新日志
    /// </summary>
    public Dictionary<string,ItemSpawnData>  Items;
    
    public LevelData()
    {
        Name = "NONE_NAME_LEVEL";
        Tags = [];
    }
    
    /// <summary>
    /// 传入新数据与当前数据合并
    /// </summary>
    /// <param name="data">新数据</param>
    public void Merge(LevelData data)
    {
        foreach (var tag in data.Tags)
        {
            Tags.Add(tag);
        }
    }
}