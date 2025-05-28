/*
 * @Author: MaoT
 * @Description: 场景对象，用于管理、加载关卡等
 */

using System.Threading.Tasks;
using Godot;
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
    
    public async Task LoadLevel(string levelName)
    {
        CurLevel = await ResourceHelper.LoadPacked<Level>($"res://Level/{levelName}.tscn", Level);
        if (CurLevel != null)
        {
            CurLevel.Init();
        }
    }
}