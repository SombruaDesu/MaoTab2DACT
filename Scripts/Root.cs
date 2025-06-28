/*
 * @Author: MaoT
 * @Description: 根对象
 */

using Godot;
using MaoTab.Scripts.Component;
using MaoTab.Scripts.Panel;
using MaoTab.Scripts.System;
using YarnSpinnerGodot;

namespace MaoTab.Scripts;

/* 欢迎阅读源码 ━(*｀∀´*)ノ亻! ，先放龙图在这里
  ..  ..                  ....#@@+.....+@@-................................=%@@%@@#:......          
                          ...%@@=..    :@@#.                        .....+@@@#-.-@@%..              
                          ..%@@-..  ..  #@@:                        ...+@@@*.....+@@=..             
                          .*@@+...  ....=@@*.                     ...+@@@+:......:@@#..             
                          -@@#....    ...%@@..                  ...=%@@*:.........%@@..             
                         .%@@:....    ...+@@=.                  .:#@@#:...........#@@:.             
                         -@@*.....    ...:@@%..                .=@@%-.........  ..+@@=.             
                        .#@@-..         ..*@@=..             ..*@@#.......      ..-@@+              
                        .@@%...         ...%@@:.          ...:@@@+....            :@@*              
                       .-@@*...         ...-@@@@@%%%%%%%%###%@@%:...              .@@#              
                        *@@-            ....+%%=+*******###%%#=...                .@@%..            
                     ..-%@@.            ..........    ..      ..                  .%@@..            
                   ..-@@@@#.              ........                        ..       #@@:.            
                   .#@@%--..              ......                          ..       *@@:.            
                 .=@@@=.....              ......                          ..       *@@-.            
               .:#@@*:...                   ....                          ..    ...+@@=.            
              .=@@%=..                      ....                                ...-@@+..           
             .*@@#....                      ....                                ...:@@#.            
          ...%@@+.....    ..............    ..       ...:-=++++-:...            ....%@@:..          
          ..%@@-....... .+#@@@@@@@@@@#-..        ..-*%@@@@@@@@@@@@@*:.          ....=@@#:.          
         ..%@@-.....*%#%@@@%*=----=+%@@%:.  ....=%@@@@#+-::...::-+%@@%=..       .....=@@@%#=..      
        ..#@@-....=%@@++=-...........-@@%.....-%@@*=:..............-#@@#:.      .......-+%@@@#+:..  
        .+@@+...:#@@#:......    ......+@@=...=@@%:....    ......    .-%@@-.               :=#%@@%*-.
        :@@%...-@@@=..          ......-@@*..+@@#....        ....    ...%@@-.              ....-*%@@@
        *@@-..-@@@....              ..-@@*.*@@*.....                ....@@%..             ......:%@@
       :@@%...@@@:....              ..=@@++@@*......                ....*@@-.             ....:#@@%-
       +@@=..*@@=...                 .#@@=@@#.....                  ....+@@=               .+%@@%=..
       #@@:..%@@.--.                 :@@#*@@-.....                  ....%@@..             .=@@@#-...
      .%@@...@@%=@@:.              ..%@@:%@@......         .        ...*@@+.              ..=#%@@@*:
      .@@%...%@@....               .#@@=.*@@-.....         =#=..    ..*@@*..              .....:*@@@
      .@@% ..+@@+...              :%@@=..:@@%.....         =%+..   ..#@@*..               .....+%@@@
      .%@@:...#@@+..            :*@@%-....-@@%=...          ....  .=%@@=...                 .:@@@%=:
       +@@+. ..*@@%+-:..  ..:=+#@@@+.......:*@@%*-...           .-#@@#:...                  ..:-....
       .%@@:....-#%@@@@@@@@@@@@%#=...........:*%@@@#*=--::::--=*%@@%=.....                 :##:.....
        -@@%:.......-+*####+=-........    ......:+#%@@@@@@@@@@@@%*:.....                   -@@*..   
        .:@@@+......        ......          ......   ..:-==-:.. ........                   .@@%.    
        ...*@@%*....        ......          ........-+##=.....  ........                   .#@@:.   
        ....:*@@@%*=:.                     ..-=*#%@@@@@#-...    ........                    =@@=    
        .......=*%@@@@%*++==--------==++*#%@@@@@%#*+-:....                                 .:@@#.   
          .........-+*#%%@@@@@@@@@@@@@%%%%#*+=:.........                ....                .#@@:.  
            ....        ....:::::::... .......      ....                ....                .-@@*.. 
                          ..........     .....                          ....                ..%@@.. 
                        .....:%@#...                                    ....                ..=@@+. 
                          ..=@@%-...                                    ....                ...%@@:.
                          .+@@#:..                                      ....                ...=@@+.
                         .*@@*....                                        ....              ....%@@:
                        .%@@+.....                                        ..                ....=@@*
                       .%@@=......                                        ..                .....%@@
                     .:%@@-.......                                                            ...-@@
                     :%@@-....                                                                ....%@
                   .:%@@-..                                                                    ...=@
                  .:%@@-...                                                                    ....@
                  .%@@:...                                                                     ....*
                 .%@@-..                                                                        ...:
                .%@@-...                                                                          ..
 */

/// <summary>
/// 根对象，游戏场景的最父级，程序的起点
/// </summary>
public partial class Root : Node
{
    public static Root Instance;
    
    private Player _player;
    
    [Export] private WeatherMgr _weatherMgr;
    [Export] private Scene _scene;
    [Export] private Interface _ui;
    [Export] private DevPanel _devUi;
    [Export] private HUD _hud;
    
    [Export] private Component.AudioMixPlayer _audioMixPlayer;  // 临时在这里控制音效节点
    
    private bool gameStarted = false;
    
    private NetworkManager _networkManager;
    
    private Logic _logic = new();
    
    public override async void _Ready()
    {
        Instance = this;
        
        _devUi.Init();
        
        Game.SoundVolumeScale = 0.15f;
        _audioMixPlayer.Init();
        
        Game.Scene = _scene;
        var camera = await ResourceHelper.LoadPacked<Camera>("res://ScenePacked/Camera.tscn", _scene);
        Game.Camera = camera;

        Game.WeatherMgr = _weatherMgr;
        _weatherMgr.Init(camera);
        
        Game.Interface = _ui;
        Game.Interface.Init();
        
        // ReSharper disable once JoinDeclarationAndInitializer
        YarnProject yarnProject;
        yarnProject = ResourceLoader.Load<YarnProject>("res://Story.yarnproject");
        // 运行时编译 Yarn
        // var yarnCompiler = new YarnCompiler();
        // yarnProject = yarnCompiler.Compile("title: Node_Start\nposition: 273,433\n---\nNull\n===\n\n\ntitle: Node_A\nposition: 273,433\n---\nHello\n===\n");
        var yarn = new YarnRuntime();
        Game.Yarn = yarn;
        yarn.Init(yarnProject);
        yarn.PlayNode("Node_Start");
     
     
        _logic.Init();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Game.Tick(delta);
        _logic.Tick();
        _hud.Tick();
    }
}