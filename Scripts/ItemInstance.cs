/*
 * @Author: MaoT
 * @Description: 场景物品对象
 */


using System.Threading;
using System.Threading.Tasks;
using Godot; 

namespace MaoTab.Scripts;

[GlobalClass]
public partial class ItemInstance : RigidBody2D
{
    [Export] public  int            W = 1;
    [Export] public  int            H = 1;
    [Export] public  ItemDefinition Def;
    
    public           bool           Rotated; // true = 旋转 90°
    public           int            X;       // 左下角格子的 x
    public           int            Y;       // 左下角格子的 y
    [Export] private Area2D         _interactionArea;
    [Export] private Area2D         _collisionArea;
    public           bool           canPackup;
    public           bool           canDrop;

    public void Init(ItemDefinition def)
    {
        Def          = def;
        Def.Instance = this;
        
        if (!Def.IsNew)
        {
            Position = Def.Position;
        }
        else
        {
            Def.IsNew  = false;
        }
        
        _interactionArea.BodyEntered += body =>
        {
            if (body is Player player)
            {
                player.SetCanPickupItem(this);
            }
            canPackup =  true;
        };
        
        _interactionArea.BodyExited += body =>
        {
            if (body is Player)
            {
                canPackup = false;
            }
        };
    }

    /// <summary>
    /// 抛
    /// </summary>
    /// <param name="power">冲量</param>
    public void Dropped(Vector2 power)
    {
        SetCollisionLayerValue(4,true);
        CallDeferred("reparent",Game.Scene.CurLevel);
        SetDeferred("freeze", false);
        LinearVelocity  = power;
        var a = new RandomNumberGenerator();
        AngularVelocity = a.RandfRange(0,90);
        
        canPackup                   = true;
        _interactionArea.Monitoring = true;
    }
    
    const int CELL = 8;  // 一格 8px
    public async Task PickedUp(Node2D node,int x,int y,bool rotated)
    {
        Freeze  = true;
        Rotated = rotated;
        X = x;  Y = y;
        
        Reparent(node);

        // 实际占用的格子尺寸
        var   sz    = GetSize(); // 已考虑旋转 (X,Y)
        float halfW = sz.X * CELL * 0.5f;
        float halfH = sz.Y * CELL * 0.5f;

        // 网格 → 像素；再减去半宽半高，让中心对到格子左下角
        float px = (x - 1) * CELL + halfW;
        float py = -(y - 1) * CELL - halfH;

        canPackup                   = false;
        _interactionArea.Monitoring = false;
        SetCollisionLayerValue(4,false);

        if (rotated)
        {
            await SmoothMoveAndRotateAsync(new Vector2(-px - 4, py - 4), 90, 0.20f);
        }
        else
        {
            await SmoothMoveAndRotateAsync(new Vector2(-px - 4, py - 4), 0, 0.20f);
        }
        
        // 位置完全就位后才可以丢下
        canDrop = true;
        /*Position = new Vector2(-px - 4, py - 4);
        RotationDegrees = rotated ? 90 : 0;*/
    }
    
    
    private async Task SmoothMoveAndRotateAsync(
        Vector2           targetPos, float targetRot, float duration,
        CancellationToken token = default)
    {
        Vector2 startPos = Position;
        float   startRot = RotationDegrees;

        float elapsed = 0f;

        // 开启 process，让 GetProcessDeltaTime 返回有效值
        SetProcess(true);

        while (elapsed < duration && !token.IsCancellationRequested)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            float delta = (float)Game.PhysicsDelta;   // 上一帧时长
            elapsed += delta;

            float t = Mathf.Clamp(elapsed / duration, 0f, 1f);

            Position        = startPos.Lerp(targetPos, t);
            RotationDegrees = Mathf.LerpAngle(startRot, targetRot, t);
        }

        // 结束时确保精确落点 / 角度
        Position        = targetPos;
        RotationDegrees = targetRot;

        SetProcess(false); // 若不再需要 _Process，可关闭
    }
    
    public Vector2I GetSize()
    {
        // Rotated == true 说明物品在网格里是旋转 90° 的
        return Rotated
            ? new Vector2I(Def.Size.Y, Def.Size.X)   // 交换 X / Y
            : Def.Size;
    }
}