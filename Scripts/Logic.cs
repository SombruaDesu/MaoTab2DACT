/*
 * @Author: MaoT
 * @Description: 逻辑，处理游戏的所有耦合逻辑
 */

using System.Threading;
using System.Threading.Tasks;
using Godot;
using MaoTab.Scripts.Panel;
using MaoTab.Scripts.System;

namespace MaoTab.Scripts;

public partial class Logic
{
    protected bool   gameStarted;
    protected Player player;
    private   string jumpKey;
    public async Task Init()
    {
        var networkManager = new NetworkManager();

        player = await ResourceHelper.LoadPacked<Player>("res://ScenePacked/Player.tscn", Game.Scene);

        Game.MainPlayer = player;
        Game.Camera.FollowTarget(player);

        var home = Game.Interface.HomePanel;

        home.OnHostGame += async void (key) =>
        {
            jumpKey      = key;
            home.Visible = false;

            await Game.Interface.LoadStart();

            player.Init(true);

            await Game.Scene.LoadLevel("TestLevel");

            networkManager.Init(true);

            gameStarted = true;

            int optTime;

            Game.Yarn.OnLineArrival += async void (s) =>
            {
                CancellationTokenSource cancellationToken = new();
                optTime = -1;

                foreach (var attribute in s.TextWithoutCharacterName.Attributes)
                {
                    switch (attribute.Name)
                    {
                        case "wait":
                            if (attribute.Properties.TryGetValue("time", out var bProperty))
                            {
                                optTime = bProperty.IntegerValue;
                            }
                            break;
                    }
                }

                void Continue()
                {
                    cancellationToken.Cancel();
                    Game.Yarn.Continue();
                }
                
                if (optTime > 0)
                {
                    _ = Game.Interface.DlgPanel.WaitDlg(() =>
                    {
                        Continue();
                    }, optTime, cancellationToken);
                }

                await Game.Interface.DlgPanel.FreshDlg(s.TextWithoutCharacterName.Text,
                    () =>
                    {
                        Continue();
                    });
            };

            Game.Yarn.OnOptionsArrival += async void (s) =>
            {
                await Game.Interface.DlgPanel.FreshOpt(s, id => { Game.Yarn.SelectedOption(id); });
            };

            await Game.Interface.LoadOver();
        };

        home.OnJoinGame += async void () =>
        {
            home.Visible = false;

            player.Init(true);

            await Game.Scene.LoadLevel("TestLevel");

            Game.Camera.FollowTarget(player);

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

        if (Input.IsActionPressed(jumpKey))
        {
            direction.Y += 1; // 跳跃
        }

        /*if (Input.IsActionPressed("j"))
        {
            player.AttackInput();
        }*/

        if (Input.IsActionJustReleased("f"))
        {
            player.InteractionInput();
        }

        player.Input(direction, Input.IsActionPressed("alt"), Input.IsActionPressed("shift"));
        player.Tick();

        Game.WeatherMgr.Tick();
        Game.Scene.Tick();
        
        // 网络同步
        if (Game.OtherPlayer != null)
        {
            Game.OtherPlayer.AsyncTick();
        }
    }
}