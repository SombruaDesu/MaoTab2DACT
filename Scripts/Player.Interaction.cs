/*
 * @Author: MaoT
 * @Description: 玩家对象，交互接口部分
 */

using System;

namespace MaoTab.Scripts;

public partial class Player
{
    private InteractionAction _curInteractionAction;
    
    public void DoInteractionAction(InteractionAction action)
    {
        // 角色没有事件时才挂载
        if (_curInteractionAction == null)
            _curInteractionAction = action;
    }
    
    public void InteractionInput()
    {
        if (_curInteractionAction != null)
        {
            switch (_curInteractionAction.Type)
            {
                case InteractionType.Yarn:
                    if(_curInteractionAction.Info != string.Empty)
                        Game.Yarn.PlayNode(_curInteractionAction.Info);
                    break;
            }
        }
    }
}