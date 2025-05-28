using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using MaoTab.Scripts.System;

namespace MaoTab.Scripts;

public class NetworkManager
{
    // 定义消息类型，用来区分不同的数据包
    public enum MessageType : byte
    {
        Greeting = 0,
        SyncData = 1
    }
    
    // 是否为主机（服务器），否则为客户端
    public bool IsHost = true;

    // 公共配置参数
    private const int    Port          = 9050;
    private const string ConnectionKey = "SomeConnectionKey";
    private const int    MaxClients    = 2;

    // LiteNetLib 对象
    private NetManager            netManager;
    private EventBasedNetListener listener;
    
    private bool _stop;
    
    public void ShutDown()
    {
        _stop = true;
    }
    
    public void Init(bool isHost)
    {
        IsHost = isHost;
        _stop  = false;
        // 初始化事件监听器
        listener = new EventBasedNetListener();

        // 统一注册网络数据接收回调（无论客户端或服务器均可接收同步数据）
        listener.NetworkReceiveEvent += OnNetworkReceive;

        // 根据模式启动对应的方法
        if (IsHost)
        {
            RunServerAsync();
        }
        else
        {
            RunClientAsync();
        }
    }

    // 异步方法实现服务器功能
    private async void RunServerAsync()
    {
        GD.Print("启动服务器……");
        netManager = new NetManager(listener);
        // 启动服务器监听，指定端口
        if (!netManager.Start(Port))
        {
            GD.PrintErr("服务器启动失败！");
            return;
        }
        GD.Print($"服务器成功启动，监听端口: {Port}");

        // 处理新的连接请求
        listener.ConnectionRequestEvent += request =>
        {
            if (netManager.GetPeersCount(ConnectionState.Connected) < MaxClients)
                request.AcceptIfKey(ConnectionKey);
            else
                request.Reject();
        };

        // 当有玩家连接后触发
        listener.PeerConnectedEvent += async peer =>
        {
            GD.Print("新玩家连接: " + peer.RemoteId);
            // 发送问候消息给新连接的玩家
            NetDataWriter writer = new NetDataWriter();
            writer.Put((byte)MessageType.Greeting);
            writer.Put("Hello client!");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            
            Game.OtherPlayer = await ResourceHelper.LoadPacked<Player>("res://ScenePacked/Fox.tscn", Game.Scene);
            Game.OtherPlayer.Init(Vector2.Zero,false);
        };

        int syncFrame = 0;
        // 异步轮询网络事件并实时同步数据
        while (!_stop)
        {
            netManager.PollEvents();

            // 构建同步数据包
            NetDataWriter syncWriter = new NetDataWriter();
            syncWriter.Put((byte)MessageType.SyncData);
            syncWriter.Put(syncFrame);
            // 模拟数据，例如一个位置值，可替换为实际游戏数据
            
            PlayerData data = Game.MainPlayer.Data;
            syncWriter.Put(data);

            // 将同步数据发送给所有已连接的客户端
            foreach (var peer in netManager.ConnectedPeerList)
            {
                peer.Send(syncWriter, DeliveryMethod.Unreliable);
            }
            syncFrame++;
            await Task.Delay(15);
        }

        netManager.Stop();
        GD.Print("服务器已停止。");
    }
    
    // 异步方法实现客户端功能
    private async void RunClientAsync()
    {
        GD.Print("启动客户端……");
        netManager = new NetManager(listener);
        if (!netManager.Start())
        {
            GD.PrintErr("客户端启动失败！");
            return;
        }
        GD.Print("客户端启动成功，正在尝试连接……");

        // 发起连接请求（此处使用 localhost，可根据需要替换为服务器 IP 或域名）
        netManager.Connect("localhost", Port, ConnectionKey);

        int syncFrame = 0;
        // 客户端也可定时发送自己的同步数据到服务器（例如玩家自己的状态）
        while (!_stop)
        {
            netManager.PollEvents();

            NetDataWriter syncWriter = new NetDataWriter();
            syncWriter.Put((byte)MessageType.SyncData);
            syncWriter.Put(syncFrame);
            
            PlayerData data = Game.MainPlayer.Data;
            syncWriter.Put(data);

            if (netManager.ConnectedPeerList.Count > 0)
            {
                netManager.ConnectedPeerList[0].Send(syncWriter, DeliveryMethod.Unreliable);
            }
            syncFrame++;
            await Task.Delay(15);
        }

        netManager.Stop();
        GD.Print("客户端已停止。");
    }
    
    // 统一数据接收处理方法，解析收到的消息
    private async void OnNetworkReceive(NetPeer fromPeer, NetDataReader dataReader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            // 确保至少有一个字节可读取（消息类型）
            if (dataReader.AvailableBytes < 1)
            {
                GD.Print("收到数据长度不足，忽略该数据包");
                dataReader.Clear();
                return;
            }

            byte messageType = dataReader.GetByte();
            if (messageType == (byte)MessageType.Greeting)
            {
                // 写入问候消息的字符串应至少包含两个字节的长度信息
                if (dataReader.AvailableBytes < 2)
                {
                    GD.Print("问候消息数据不全，忽略");
                    dataReader.Clear();
                    return;
                }
                // 尝试解析字符串，若内容为空，则 GetString 返回空字符串而非 null 
                string greeting = dataReader.GetString(100);
                GD.Print("收到来自 " + fromPeer.RemoteId + " 的问候: " + greeting);
                
                Game.OtherPlayer  = await ResourceHelper.LoadPacked<Player>("res://ScenePacked/Fox.tscn", Game.Scene);
                Game.OtherPlayer .Init(Vector2.Zero,false);
            }
            else if (messageType == (byte)MessageType.SyncData)
            {
                // 检查剩余字节是否足够（一个 int 和一个 float）
                if (dataReader.AvailableBytes < sizeof(int) + sizeof(float))
                {
                    GD.Print("同步数据包数据不全，忽略");
                    dataReader.Clear();
                    return;
                }
                int        frame    = dataReader.GetInt();
                PlayerData data = dataReader.Get<PlayerData>();
             
                 // 同步角色的动画状态等
                 Game.OtherPlayer.Async(data);
                
                GD.Print($"接收到来自 {fromPeer.RemoteId} 的同步数据: 帧号={frame}, 位置={data.Position}");
            }
            else
            {
                GD.Print("收到未知类型消息，来自 " + fromPeer.RemoteId);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("处理网络消息时出错: " + ex.Message);
        }
        finally
        {
            // 确保数据缓冲归还
            dataReader.Clear();
        }
    }
}