using Godot;

public class PointInfo
{
    public long   PointID;
    public Vector2 Position;

    public bool IsLeftEdge  = false;
    public bool IsRightEdge = false;
    public bool IsLeftWall  = false;
    public bool IsRightWall = false;
    public bool IsFallTile  = false;
    public bool IsPositionPoint = false;     // 起终点虚拟节点

    public PointInfo(long id, Vector2 pos)
    {
        PointID  = id;
        Position = pos;
    }
}