using System;
using Godot;

namespace MaoTab.Scripts.Panel;

public partial class HUD : Control
{
    [Export] private TextureProgressBar Bar;
    [Export] private AnimatedSprite2D   emo;
    private          float              _targetProgress;
    private          float              curProgress;
    public void Tick()
    {
        curProgress = Mathf.Lerp(curProgress, 1 - Game.WeatherStrength / 100,  (float)Game.PhysicsDelta * 0.01f);
        Bar.Value   = MathfHelper.DLerp(Bar.Value, 100 - Game.WeatherStrength,curProgress);
        
        switch (Game.WeatherStrength)
        {
            case  >= 0 and < 30:
                emo.Play("1");
                break;
            case  >= 30 and < 60:
                emo.Play("2");
                break;
            case  >= 60 and < 80:
                emo.Play("3");
                break;
        }
    }
}