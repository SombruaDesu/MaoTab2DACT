/*
 * @Author: MaoT
 * @Description: 对话界面
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Component;
using MaoTab.Scripts.System;
using YarnSpinnerGodot;

namespace MaoTab.Scripts.Panel;

public partial class DlgPanel : Control
{
    [Export] private TypingEffectText DlgTextLabel;

    [Export] private Button continueMask;
    
    [Export] private VBoxContainer optBox;
    
    [Export] private ProgressBar dlgWaitBar;
    
    [Export] private AnimationAsyncPlayer movieAnimationPlayer;
    
    [Export] private AnimationAsyncPlayer illPlayer;
    
    public void Init()
    {
        dlgWaitBar.Visible = false;
        
        continueMask.ButtonUp += () =>
        {
            _onContinue?.Invoke();
            _onContinue = null;
        };
    }
    
    private bool _isShow;
    
    public async Task WaitDlg(
        Action action,
        float  seconds,
        CancellationTokenSource cancellationToken = null)
    {
        if(cancellationToken == null) return;
        
        dlgWaitBar.Visible = true;
        
        try
        {
            float elapsed = 0f;
            while (elapsed < seconds && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken.Token);

                elapsed += (float)Game.PhysicsDelta;
                dlgWaitBar.Value = MathF.Min((float)dlgWaitBar.MaxValue,
                    (float)dlgWaitBar.MaxValue * (elapsed / seconds));
            }
        }
        catch (OperationCanceledException)
        {
            dlgWaitBar.Visible = false;
        }
        
        if(cancellationToken.IsCancellationRequested) return;
        
        action?.Invoke();
    }
    
    public async Task ShowDlg()
    {
        _isShow = true;
        continueMask.Visible = true;
        await movieAnimationPlayer.PlayAsync("Show");
        await movieAnimationPlayer.PlayAsync("Show");
    }

    public async Task HideDlg()
    {
        await movieAnimationPlayer.PlayAsync("Hide");
        await movieAnimationPlayer.PlayAsync("Hide");
        _isShow              = false;
        continueMask.Visible = false;
    }

    private Action _onContinue;

    private List<DlgOpt> _opts = new();
    public void ClearDlgOpt()
    {
        foreach (var opt in _opts)
        {
            opt.QueueFree();
        }
        
        _opts.Clear();
    }
    
    /// <summary>
    /// 刷新对话文本
    /// </summary>
    /// <param name="line">对话内容</param>
    /// <param name="onContinue">玩家输入继续时</param>
    public async Task FreshDlg(string line,Action onContinue)
    {
        ClearDlgOpt();
        await ShowDlg();
        await DlgTextLabel.Fresh(line, () =>
        {
            _onContinue = onContinue;
        });
    }

    public async Task FreshOpt(DialogueOption[] opts,Action<int> onSelect)
    {
        for (var index = 0; index < opts.Length; index++)
        {
            var opt     = opts[index];
            var optNode = await ResourceHelper.LoadPacked<DlgOpt>("res://Panel/Dlg/DlgOpt.tscn", optBox);
            _opts.Add(optNode);
            var index1  = index;
            
            CancellationTokenSource cancellationToken = new();
            
            optNode.Fresh(opt.Line.TextWithoutCharacterName.Text, () =>
            {
                onSelect?.Invoke(index1);
                ClearDlgOpt();
            });
            
            foreach (var attribute in opt.Line.TextWithoutCharacterName.Attributes)
            {
                switch (attribute.Name)
                {
                    case "wait":
                        if (attribute.Properties.TryGetValue("time", out var bProperty))
                        {
                            optNode.WaitOpt(() =>
                            {
                                cancellationToken.Cancel();
                                onSelect?.Invoke(index1);
                            },
                            bProperty.IntegerValue, // 自动选择时间
                            cancellationToken);
                        }
                        break;
                }
            }
        }
    }
}