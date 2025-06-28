/*
 * @Author: MaoT
 * @Description: 主菜单界面
 */

using System;
using Godot;

namespace MaoTab.Scripts.Panel;

public partial class HomePanel : Control
{
    [Export] private Button _jButton;
    [Export] private Button _spaceButton;
    [Export] private Button _wButton;
    
    public void Init()
    {
        _jButton.ButtonUp += () =>
        {
            OnHostGame?.Invoke("j");
        };
        
        _spaceButton.ButtonUp += () =>
        {
            OnHostGame?.Invoke("space");
        };
        
        _wButton.ButtonUp += () =>
        {
            OnHostGame?.Invoke("w");
        };
    }

    public Action<string> OnHostGame;
    public Action OnJoinGame;
}