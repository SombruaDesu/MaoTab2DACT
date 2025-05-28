/*
 * @Author: MaoT
 * @Description: 界面管理器，用于管理所有的UI
 */

using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Panel;
using MaoTab.Scripts.System;

namespace MaoTab.Scripts;

/// <summary>
/// 界面管理器，用于管理所有的UI
/// </summary>
public partial class Interface : Control
{
    public HomePanel HomePanel;
    
    public async Task Init()
    {
        HomePanel = await ResourceHelper.LoadPacked<HomePanel>("res://Panel/HomePanel.tscn",this);
        HomePanel.Init();
    }
}