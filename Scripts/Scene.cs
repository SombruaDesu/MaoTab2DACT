/*
 * @Author: MaoT
 * @Description: 场景对象，用于管理、加载关卡等
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Component;

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
        
        GD.Print("----\n增加关卡标签" +
                 "\n\t关卡："+ levelName + 
                 "\n\t标签：" + tagName);
    }

    /// <summary>
    /// 检测指定关卡数据是否存在标签
    /// </summary>
    /// <param name="levelName">关卡名称</param>
    /// <param name="tagName">标签名称</param>
    /// <returns>true存在</returns>
    /// <para>请勿在Gds调用此函数以时进行断点调试，断点会导致Gds丢失回调报错</para>
    public bool HasTag(string levelName, string tagName)
    {
        if (levelName == "this")
        { 
            levelName = CurLevel.Data.Name;
        }
        
        if (AllLevelData.TryGetValue(levelName, out var data))
        {
            var result = data.Tags.Contains(tagName);
            GD.Print("----\n检测关卡标签是否存在" +
                     "\n\t关卡："+ levelName + 
                     "\n\t标签：" + tagName + 
                     "\n\t结果：" + result);
            return result;
        }

        GD.Print("----\n检测关卡标签是否存在" +
                 "\n\t关卡："+ levelName + 
                 "\n\t标签：" + tagName + 
                 "\n\t结果：关卡数据不存在，默认 false");

        return false;
    }
    
    
    public async Task ChangeLevel(string levelName,string spawnPointName = "")
    {
        if(CurLevel.Data.Name == levelName) return;
        
        PrvLevel = CurLevel; // 缓存上一个关卡对象，方便加载关卡后卸载
        
        await Game.Interface.LoadStart();
        
        if (await LoadLevel(levelName,spawnPointName))
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
    
    public Task<bool> LoadLevel(string levelName,string spawnPointName = "")
    {
        GD.Print("----\n开始加载关卡：" + levelName);
        
        var packed = ResourceLoader.Load<PackedScene>($"res://Level/{levelName}.tscn");
        var node = packed?.Instantiate();
        if (node is Level level)
        {
            level.Init();
            
            Game.WeatherMgr.Refresh(level.SeaFace);
            
            if (level.SpawnPoint == null || level.SpawnPoint.Count == 0)
            {
                GD.PrintErr("----\n关卡加载失败：" + levelName + " 关卡不存在玩家刷新位置");
                return Task.FromResult(false);
            }
            
            CurLevel = level;
            Level.AddChild(level);
        }
        else
        {
            GD.PrintErr("----\n关卡加载失败：" + levelName + " 关卡对象实例化失败");
            return Task.FromResult(false);
        }
        
        // 检测是否存在（未生成时的）关卡数据，如果存在，则把刚生成的关卡数据与（未生成时的）关卡数据合并
        if (AllLevelData.TryGetValue(levelName, out var data))
        {
            CurLevel.Data.Merge(data);
        }
        // 如果不存在数据，则正常初始化关卡
        else
        {
            AllLevelData.Add(levelName, CurLevel.Data);
        }

        // 如果玩家存在则把它丢到关卡的刷新点位
        if (Game.MainPlayer != null)
        {
            if (string.IsNullOrWhiteSpace(spawnPointName))
            {
                // 如果刷新点没有设置，则默认使用第一个出生点
                var pos = CurLevel.SpawnPoint.FirstOrDefault().Value.Position;
                Game.MainPlayer.Position = pos;
                GD.PrintRich("----\n[color=#e16032]" +
                             "关卡加载完成：" + levelName + 
                             "，由于关卡没有设置玩家刷新位置" +
                             "，使用默认刷新点：" + CurLevel.SpawnPoint.FirstOrDefault().Key +
                             "，位置：" + pos);
            }
            else
            {
                if (CurLevel.SpawnPoint.ContainsKey(spawnPointName))
                {
                    var pos = CurLevel.SpawnPoint[spawnPointName].Position;
                    Game.MainPlayer.Position = pos;
                    GD.PrintRich("----\n[color=#57b289]" +
                                 "关卡加载完成：" + levelName + 
                                 "，关卡加载完成，" + "使用刷新点：" + CurLevel.SpawnPoint.FirstOrDefault().Key +
                                 "，位置：" + pos);
                }
                else
                {
                    // 如果刷新点没有找到，也默认使用第一个出生点
                    var pos = CurLevel.SpawnPoint.FirstOrDefault().Value.Position;
                    Game.MainPlayer.Position = pos;
                    GD.PrintRich("----\n[color=#e16032]" +
                                 "关卡加载完成：" + levelName + 
                                 "，由于没有找到设置的玩家刷新位置：" + spawnPointName +
                                 "，使用默认刷新点：" + CurLevel.SpawnPoint.FirstOrDefault().Key +
                                 "，位置：" + pos);
                }
            }
        }
        
        return Task.FromResult(true);
    }
}