/*
 * @Author: MaoT
 * @Description: 关卡对象，用于管理关卡内实体
 */

using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class Level : Node
{
    public LevelData Data;

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
        Data.Tags = [];
    }

    [Export] private string LevelName;
    [Export] public Node2D SpawnPoint;
}