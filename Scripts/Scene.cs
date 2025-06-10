/*
 * @Author: MaoT
 * @Description: 场景对象，用于管理、加载关卡等
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Component;
using MaoTab.Scripts.System;

namespace MaoTab.Scripts;

/// <summary>
/// 场景对象，用于管理、加载关卡等
/// </summary>
public partial class Scene : Node2D
{
    [Export] public Node2D Level;

    /// <summary>
    /// 当前生效关卡（整个游戏同时只有一个）
    /// </summary>
    public Level CurLevel;

    /// <summary>
    ///  加载前的上一个关卡
    /// </summary>
    public Level PrvLevel;
    
    public Dictionary<string, LevelData> AllLevelData = new();

    /// <summary>
    /// 向指定关卡数据添加标签
    /// </summary>
    /// <param name="levelName">关卡名称</param>
    /// <param name="tagName">标签名称</param>
    public void AddTag(string levelName, string tagName)
    {
        if (levelName == "this")
        {
            if (CurLevel == null)
            {
                return;
            }

            levelName = CurLevel.Data.Name;
        }

        // 检测是否已经存在关卡数据，如果存在则向现有数据插入标签
        if (AllLevelData.TryGetValue(levelName, out var data))
        {
            data.Tags.Add(tagName);
        }
        // 如果不存在关卡数据，则新建关卡数据，并插入标签（即使是关卡没有生成也能提前定义数据）
        else
        {
            AllLevelData.Add(levelName, new LevelData
            {
                Tags = { tagName }
            });
        }
        
        GD.Print("AddTag"+ levelName + tagName);
    }

    /// <summary>
    /// 检测指定关卡数据是否存在标签
    /// </summary>
    /// <param name="levelName">关卡名称</param>
    /// <param name="tagName">标签名称</param>
    /// <returns>true存在</returns>
    public bool HasTag(string levelName, string tagName)
    {
        if (levelName == "this")
        {
            if (CurLevel == null)
            {
                return false;
            }

            levelName = CurLevel.Data.Name;
        }
        
        if (AllLevelData.TryGetValue(levelName, out var data))
        {
            return data.Tags.Contains(tagName);
        }

        return false;
    }

    public async Task ChangeLevel(string levelName)
    {
        if(CurLevel.Data.Name == levelName) return;
        
        PrvLevel = CurLevel; // 缓存上一个关卡对象，方便加载关卡后卸载
        
        await Game.Interface.LoadStart();

        if (await LoadLevel(levelName))
        {
            GameState.ClearCacheData(); // 清理上一个关卡内产生的缓存数据
            await UnloadLevel();
        }
        
        await Game.Interface.LoadOver();
    }

    private Task UnloadLevel()
    {
        if (PrvLevel == null)
        {
           return Task.CompletedTask;
        }
        
        PrvLevel.QueueFree();
        PrvLevel = null;
        return Task.CompletedTask;
    }
    
    public async Task<bool> LoadLevel(string levelName,string spawnPointName = "")
    {
        CurLevel = await ResourceHelper.LoadPacked<Level>($"res://Level/{levelName}.tscn", Level);

        if (CurLevel == null) return false;
        
        // 检测是否存在（未生成时的）关卡数据，如果存在，则把刚生成的关卡数据与（未生成时的）关卡数据合并
        if (AllLevelData.TryGetValue(levelName, out var data))
        {
            CurLevel.Data.Merge(data);
        }
        // 如果不存在数据，则正常初始化关卡
        else
        {
            CurLevel.Init();
            AllLevelData.Add(levelName, CurLevel.Data);
        }

        // 如果玩家存在则把它丢到关卡的刷新点位
        if (Game.MainPlayer != null)
        {
            if (spawnPointName == "")
            {
                Game.MainPlayer.Position = CurLevel.SpawnPoint.Position;
            }
        }
        
        return true;
    }
}