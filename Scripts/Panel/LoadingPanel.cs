/*
 * @Author: MaoT
 * @Description: 加载界面
 */

using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Component;

namespace MaoTab.Scripts.Panel;

public partial class LoadingPanel : Control
{
    [Export] AnimationAsyncPlayer  animationPlayer;
    
    private bool _isLoading;

    public void Init()
    {
        
    }

    public async Task Load()
    {
        if(_isLoading) return;
        _isLoading = true;
        
        await animationPlayer.PlayAsync("Show");
    }

    public async Task LoadOver()
    {
        if (!_isLoading) return;
        _isLoading = false;
        
        await animationPlayer.PlayAsync("Leave");
    }
}