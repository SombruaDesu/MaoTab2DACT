/*
 * @Author: MaoT
 * @Description: 逻辑，处理游戏的所有耦合逻辑
 */

using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Panel;
using MaoTab.Scripts.System;

namespace MaoTab.Scripts;

public partial class Logic
{
    protected bool   gameStarted;
    protected Player player;

    public async Task Init()
    {
        var networkManager = new NetworkManager();

        player = await ResourceHelper.LoadPacked<Player>("res://ScenePacked/Fox.tscn", Game.Scene);

        Game.MainPlayer          = player;
        Game.Camera.FollowTarget = player;

        await Game.Scene.LoadLevel("TestLevel");
        var level = Game.Scene.CurLevel;
        
        var home  = Game.Interface.HomePanel;

        home.OnHostGame += async void () =>
        {
            home.Visible = false;
            player.Init(level.SpawnPoint.Position, true);

            networkManager.Init(true);

            gameStarted = true;

            var dlg = await ResourceHelper.LoadPacked<DlgPanel>("res://Panel/DlgPanel.tscn", Game.Interface);
            dlg.Init();

            /*Game.Yarn.OnLineArrival += s => 
            { 
                _ = dlg.FreshDlg(s, () =>
                {
                    
                });
            };*/
        };

        home.OnJoinGame += () =>
        {
            home.Visible = false;

            player.Init(level.SpawnPoint.Position, true);

            Game.Camera.FollowTarget = player;

            networkManager.Init(false);

            gameStarted = true;
        };
    }

    public void Tick()
    {
        if (!gameStarted) return;

        Game.Camera.Tick();

        var direction = new Vector2();

        // 检查 WASD 键的输入
        if (Input.IsActionPressed("a"))
        {
            direction.X -= 1; // 向左
        }

        if (Input.IsActionPressed("d"))
        {
            direction.X += 1; // 向右
        }

        if (Input.IsActionPressed("w"))
        {
            direction.Y += 1; // 跳跃
        }

        if (Input.IsActionPressed("j"))
        {
            player.AttackInput();
        }
        
        if (Input.IsActionPressed("f"))
        {
            player.InteractionInput();
        }

        player.Input(direction, Input.IsActionPressed("shift"));
        player.Tick();

        if (Game.OtherPlayer != null)
        {
            Game.OtherPlayer.AsyncTick();
        }
    }
}