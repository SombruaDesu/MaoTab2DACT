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
    const int CELL = 8;  // 一格 8px
    
    [Export] public  ItemDefinition Def;
    
    [Signal] delegate void OnPickedUpEventHandler();
    
    public           bool           Rotated; // true = 旋转 90°
    public           int            X;       // 左下角格子的 x
    public           int            Y;       // 左下角格子的 y
    [Export] private Area2D         _interactionArea;
    [Export] private Area2D         _collisionArea;
    public           bool           canPackup;
    public           bool           canDrop;

    public override void _Ready()
    {
        if(Def != null)
          Init(Def);
    }

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

    public async Task PlaceTo(Vector2 targetPos,float targetRot, float duration
                             ,Callable onPickedUp,CancellationToken token = default)
    {
        Reparent(Game.Scene.CurLevel.ObjectNode);
        
        Vector2 startPos = Position;
        float   startRot = RotationDegrees;
        float   elapsed  = 0f;

        SetProcess(true);

        while (elapsed < duration && !token.IsCancellationRequested)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            float delta   = (float)Game.PhysicsDelta;
            elapsed      += delta;
            float t       = Mathf.Clamp(elapsed / duration, 0f, 1f);

            // 线性插值得到“基准”点
            float x = Mathf.Lerp(startPos.X, targetPos.X, t);
            float y = Mathf.Lerp(startPos.Y, targetPos.Y, t);

            // 给 Y 加上抛物线偏移（Godot 2D 的 Y 轴向下为正，所以这里用减号）
            y -= 4f * 30f * t * (1f - t);

            Position        = new Vector2(x, y);
            RotationDegrees = Mathf.LerpAngle(startRot, targetRot, t);
        }

        Position        = targetPos;
        RotationDegrees = targetRot;
        SetProcess(false);
        
        onPickedUp.Call();
    }
    
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
        
        EmitSignal(SignalName.OnPickedUp);
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