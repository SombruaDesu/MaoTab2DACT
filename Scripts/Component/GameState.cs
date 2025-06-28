/*
 * @Author: MaoT
 * @Description: 游戏状态节点，用于与 GDS 交互游戏状态，由引擎Autoload生成
 */


using System.Collections.Generic;
using Godot;


namespace MaoTab.Scripts.Component;

/// <summary>
/// 游戏状态节点，用于与 GDS 交互游戏状态，由引擎Autoload生成
/// </summary>
[GlobalClass]
public partial class GameState : Node
{
    public static Dictionary<string,Node> CacheNodes = new();
    
    public static Godot.Collections.Dictionary<string,Variant> VariantBase = new ();

    public static void AddInt(string key,int value)
    {
        if (VariantBase.ContainsKey(key))
        {
            VariantBase[key] = VariantBase[key].AsInt32() + value;
        }
        else
        {
            VariantBase.Add(key,value);
        }
    }

    public static int TryGetInt(string key,int defaultValue)
    {
        if (VariantBase.ContainsKey(key))
        {
            return VariantBase[key].AsInt32();
        }

        VariantBase.Add(key,defaultValue);
        return defaultValue;
    }
    
    public static int GetInt(string key)
    {
        if (VariantBase.ContainsKey(key))
        {
            return VariantBase[key].AsInt32();
        }

        return 0;
    }
    
    public static Camera GetCamera()
    {
        return Game.Camera;
    }
    
    /// <summary>
    /// 清除缓存数据
    /// </summary>
    public static void ClearCacheData()
    {
        CacheNodes.Clear();
    }
    
    /// <summary>
    /// 存储节点
    /// </summary>
    /// <param name="name">命名</param>
    /// <param name="node">节点对象</param>
    public static void StorageNode(string name,Node node)
    {
        CacheNodes.TryAdd(name, node);
    }

    /// <summary>
    /// 从缓存堆里获取节点
    /// </summary>
    /// <param name="name">命名</param>
    /// <returns>对象</returns>
    public static Node PeekNode(string name)
    {
        if (CacheNodes.TryGetValue(name, out var node))
        {
            return node;
        }

        GD.PrintErr($"从缓存获取节点失败，名称：{name}");
        return null;
    }

    public static void ChangeLevel(string levelName,string pointName)
    {
        Game.MainPlayer.ClearAllActions();
        Game.Scene.ChangeLevel(levelName,pointName);
    }
    
    public static int GetGameLoop() { return Game.Loop; }
    public static Player GetPlayer() { return Game.MainPlayer; }

    /// <inheritdoc cref="Scene.HasTag"/>
    public static bool HasLevelTag(string scene,string tag)
    {
        return Game.Scene.HasTag(scene,tag);
    }

    public static void AddLevelTag(string scene, string tag)
    {
        Game.Scene.AddTag(scene,tag);
    }

    public static void RemoveLevelTag(string scene, string tag)
    {
        Game.Scene.RemoveTag(scene,tag);
    }

    
    public static Node GetLevelNode(string path)
    {
        if(Game.Scene.CurLevel == null) return null;
        var node = Game.Scene.CurLevel.GetNode(path);
        if (node == null)
        {
            GD.PrintErr($"从场景获取节点失败，路径：{path}");
        }
        return node;
    } 
}