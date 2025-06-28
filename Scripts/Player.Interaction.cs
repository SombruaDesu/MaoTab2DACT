/*
 * @Author: MaoT
 * @Description: 玩家对象，交互接口部分 Player.Interaction
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    private HashSet<YarnAction> _curYarnActionList = [];
    
    private string       _curGdsTag = string.Empty;
    private Callable     _curGdsAction;
    
    /// <summary>
    /// 当前可以拾取的物品
    /// </summary>
    private ItemInstance _canPickupItem;

    private bool allowPickup = true;

    public void ClearAllActions()
    {
        _curYarnActionList.Clear();
        ClearGdsAction();
    }
    
    public void ClearGdsAction()
    {
        _curGdsTag =  string.Empty;
    }

    public void RemoveYarnAction(YarnAction  action)
    {
        if(_curYarnActionList.Contains(action))
            _curYarnActionList.Remove(action);
    }
    
    public void SetYarnAction(YarnAction yarnAction)
    {
        _curYarnActionList.Add(yarnAction);
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
        ClearGdsAction();
    }
    
    public void DoYarnAction(YarnAction action)
    {
        if (action.Info != string.Empty)
        {
            Game.Yarn.PlayNode(action.Info);
        }

        RemoveYarnAction(action);
    }
    
    public async Task InteractionInput()
    {
        if (_canPickupItem is { canPackup: true })
        {
            allowPickup = false;
            await AutoPlace(_canPickupItem);
            allowPickup = true;
        }
        
        if (_curYarnActionList.Count > 0)
        {
            var yarnAction = _curYarnActionList.First();
            if (yarnAction.Info != string.Empty)
                Game.Yarn.PlayNode(yarnAction.Info);
        }

        if (_curGdsTag != string.Empty)
        {
            if(_curGdsAction.Target != null)
                _curGdsAction.Call();
        }
    }
}