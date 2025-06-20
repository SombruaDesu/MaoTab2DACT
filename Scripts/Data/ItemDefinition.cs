namespace MaoTab.Scripts;

public class ItemDefinition
{
    public readonly string Id;
    public readonly Size   Size;
    public readonly bool   CanRotate;

    public ItemDefinition(string id, int w, int h, bool canRotate)
    {
        Id        = id;
        Size      = new Size(w, h);
        CanRotate = canRotate;
    }
}