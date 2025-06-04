/*
 * @Author: MaoT
 * @Description: 对话界面，选项元素
 */

using System;
using Godot;

namespace MaoTab.Scripts.Panel;

public partial class DlgOpt : Control
{
    [Export] private RichTextLabel label;
    [Export] private Button button;

    public void Fresh(string text, Action onSelect)
    {
        label.Text = text;
        button.ButtonUp += () =>
        {
            onSelect?.Invoke();
        };
    }
}