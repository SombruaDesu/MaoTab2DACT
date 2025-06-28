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
    public HomePanel    HomePanel;
    public DlgPanel     DlgPanel;
    public LoadingPanel LoadingPanel;
    
    public async Task LoadStart()
    {
        await LoadingPanel.Load();
    }

    public async Task LoadOver()
    {
        await LoadingPanel.LoadOver();
    }
    
    public async Task Init()
    {
        HomePanel = await ResourceHelper.LoadPacked<HomePanel>("res://Panel/HomePanel.tscn",this);
        HomePanel.Init();
        
        DlgPanel = await ResourceHelper.LoadPacked<DlgPanel>("res://Panel/Dlg/DlgPanel.tscn", this);
        DlgPanel.Init();
        
        LoadingPanel = await ResourceHelper.LoadPacked<LoadingPanel>("res://Panel/LoadingPanel.tscn", this);
        LoadingPanel.Init();
    }
}