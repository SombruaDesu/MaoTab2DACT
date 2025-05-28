using System;
using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Component;

namespace MaoTab.Scripts.Panel;

public partial class DlgPanel : Control
{
    [Export] private TypingEffectText DlgTextLabel;

    [Export] private Button continueMask;
    
    public void Init()
    {
        Hide(false);

        continueMask.ButtonUp += () =>
        {
            _onContinue?.Invoke();
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
    
    /// <summary>
    /// 刷新对话文本
    /// </summary>
    /// <param name="line">对话内容</param>
    /// <param name="onContinue">玩家输入继续时</param>
    public async Task FreshDlg(string line,Action onContinue)
    {
        await DlgTextLabel.Fresh(line, () =>
        {
            _onContinue = onContinue;
        });
    }
}