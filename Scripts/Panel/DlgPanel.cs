/*
 * @Author: MaoT
 * @Description: 对话界面
 */


using System;
using System.Collections.Generic;
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
    
    public void Init()
    {
        Hide(false);
        
        continueMask.ButtonUp += () =>
        {
            _onContinue?.Invoke();
            _onContinue = null;
        };
    }

    public void Show(bool anim)
    {
        if (anim)
        {
            Visible = true;
        }
        else
        {
            Visible = true;
        }
    }

    public void Hide(bool anim)
    {
        if (anim)
        {
            Visible = false;
        }
        else
        {
            Visible = false;
        }
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
        Show(false);
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
            optNode.Fresh(opt.Line.TextWithoutCharacterName.Text, () =>
            {
                onSelect?.Invoke(index1);
                ClearDlgOpt();
            });
        }
    }
}