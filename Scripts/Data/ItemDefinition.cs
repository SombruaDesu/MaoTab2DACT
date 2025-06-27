/*
 * @Author: MaoT
 * @Description: 物品数据
 */


using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string Id;
    [Export] public Vector2I Size;
    [Export] public bool     CanRotate;
    [Export] public float    Weight;

    public ItemInstance Instance;
    public bool         IsNew = true;
    public Vector2 Position;
}