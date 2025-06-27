/*
 * @Author: MaoT
 * @Description: 对话界面，选项元素
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts.Panel;

public partial class DlgOpt : Control
{
    [Export] private RichTextLabel label;
    [Export] private Button        button;
    [Export] private ProgressBar   optWaitBar;

    public void Fresh(string text, Action onSelect)
    {
        label.Text      =  text;
        button.ButtonUp += () => { onSelect?.Invoke(); };
    }

    public async Task WaitOpt(
        Action                  action,
        float                   seconds,
        CancellationTokenSource cancellationToken = default)
    {
        if(cancellationToken == null) return;
        
        float elapsed = 0f;
        while (elapsed < seconds && !cancellationToken.IsCancellationRequested)
        {
            // 等待下一帧（相当于 yield return null）
            await Task.Delay(10, cancellationToken.Token);
            
            elapsed          += (float)Game.PhysicsDelta;
            optWaitBar.Value =  MathF.Min((float)optWaitBar.MaxValue, (float)optWaitBar.MaxValue * (elapsed / seconds));
        }
        
        if(cancellationToken.IsCancellationRequested) return;
        
        action?.Invoke();
    }
}