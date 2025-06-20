/*
 * @Author: MaoT
 * @Description: 玩家对象，交互接口部分 Player.Interaction
 */

using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    private YarnAction _curYarnAction;
    
    private string       _curGdsTag = string.Empty;
    private Callable     _curGdsAction;
    
    /// <summary>
    /// 当前可以拾取的物品
    /// </summary>
    private ItemInstance _canPickupItem;

    private bool allowPickup = true;
    
    public void ClearAction()
    {
        _curYarnAction = null;
        _curGdsTag =  string.Empty;
    }
    
    public void SetYarnAction(YarnAction yarnAction)
    {
        // 角色没有事件时才挂载
        if (_curYarnAction == null)
        {
            _curYarnAction = yarnAction;
        }
    }

    public void SetCanPickupItem(ItemInstance item)
    {
        _canPickupItem = item;
    }
    
    public void SetGdsAction(Callable callable)
    {
        if (_curGdsTag == string.Empty)
        {
            _curGdsTag  = callable.Method.ToString();
            _curGdsAction = callable;
        }
    }

    public void DoGdsAction(Callable callable)
    {
        callable.Call();
        ClearAction();
    }
    
    public void DoYarnAction(YarnAction action)
    {
        if (action.Info != string.Empty)
        {
            Game.Yarn.PlayNode(_curYarnAction.Info);
        }
        ClearAction();
    }
    
    public async Task InteractionInput()
    {
        if (_canPickupItem is { canPackup: true })
        {
            allowPickup = false;
            await AutoPlace(_canPickupItem);
            allowPickup = true;
        }
        
        if (_curYarnAction != null)
        {
            if (_curYarnAction.Info != string.Empty)
                Game.Yarn.PlayNode(_curYarnAction.Info);
            
        }

        if (_curGdsTag != string.Empty)
        {
            if(_curGdsAction.Target != null)
                _curGdsAction.Call();
        }
    }
}