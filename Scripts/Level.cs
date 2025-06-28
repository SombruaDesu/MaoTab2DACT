/*
 * @Author: MaoT
 * @Description: 关卡对象，用于管理关卡内实体
 */

using System;
using Godot;
using Godot.Collections;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class Level : Node
{
    public LevelData Data = new();

    [Export] private TileMapPathFind _pathLayer;
    
    [Export] public float SeaFacePosition;
    [Export] public Sprite2D SeaFace;

    [Export] public Node ObjectNode;
    
    public void AddTag(string tag)
    {
        Data.Tags.Add(tag);
    }

    public bool HasTag(string tag)
    {
        return Data.Tags.Contains(tag);
    }
    
    public void Init()
    {
        Data.Name = LevelName;

        if(_pathLayer != null)
            _pathLayer.Init();
        
        foreach (var point in _spawnPoint)
        {
            var path = point.Value;
            
            if (path != null)
            {
                if (GetNode(path) != null && GetNode(path) is Node2D node)
                {
                    SpawnPoint.Add(point.Key, node);
                }
            }
        }
    }
    
    
    [Export] private string LevelName;
    [Export] private Dictionary<string,NodePath> _spawnPoint;
    public Dictionary<string, Node2D> SpawnPoint = new();
}