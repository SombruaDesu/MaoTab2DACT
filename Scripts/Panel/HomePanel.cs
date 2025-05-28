using System;
using Godot;

namespace MaoTab.Scripts.Panel;

public partial class HomePanel : Control
{
    [Export] private Button _hostButton;
    [Export] private Button _joinButton;
    
    public void Init()
    {
        _hostButton.ButtonUp += () =>
        {
            OnHostGame?.Invoke();
        };
        
        _joinButton.ButtonUp += () =>
        {
            OnJoinGame?.Invoke();
        };
    }

    public Action OnHostGame;
    public Action OnJoinGame;
}