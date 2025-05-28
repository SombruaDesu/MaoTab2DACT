/*
 * @Author: MaoT
 * @Description: 玩家数据对象
 */

using Godot;
using LiteNetLib.Utils;

namespace MaoTab.Scripts;

public struct PlayerData : INetSerializable
{
    /// <summary>
    /// 此数据对象服务的玩家对象
    /// </summary>
    public Player Player;
    
    public Player.EAnimationState State;
    
    /// <summary>
    /// 角色朝向
    /// </summary>
    public bool Facing;
    
    /// <summary>
    /// 是否允许玩家移动
    /// </summary>
    public bool Movable;

    /// <summary>
    /// 行走速度（像素/秒）
    /// </summary>
    public float WalkSpeed;

    /// <summary>
    /// 奔跑速度（像素/秒）
    /// </summary>
    public float RunSpeed;

    /// <summary>
    /// 跳跃冲量（负向上）
    /// </summary>
    public int JumpImpulse;
    
    /// <summary>
    /// 重力加速度（像素/秒²）
    /// </summary>
    public int FallAcceleration;
    
    public Vector2 Position;
    
    /// <summary>
    /// 当前引擎物理状态下的角色向量（实时向量）
    /// </summary>
    public Vector2 Velocity => Player == null ? Vector2.Zero : new Vector2(Player.Velocity.X, Player.Velocity.Y);
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put((byte)State);
        writer.Put(Facing);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position.X = reader.GetFloat();
        Position.Y = reader.GetFloat();
        State = (Player.EAnimationState)reader.GetByte();
        Facing = reader.GetBool();
    }
}