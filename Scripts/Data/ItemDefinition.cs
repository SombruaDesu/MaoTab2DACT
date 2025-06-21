using Godot;

namespace MaoTab.Scripts;

[GlobalClass]
public partial class ItemDefinition(string id, int w, int h,float weight, bool canRotate)
    : GodotObject
{
    public readonly string   Id        = id;
    public readonly Vector2I Size      = new(w, h);
    public readonly bool     CanRotate = canRotate;
    public readonly float    Weight = weight;
}