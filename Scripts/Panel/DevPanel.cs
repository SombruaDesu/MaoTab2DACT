using Godot;

namespace MaoTab.Scripts.Panel;

public partial class DevPanel : Control
{
    [Export] private HSlider weatherSlider;
    [Export] private HSlider weatherSpeedSlider;
    [Export] private Button  LightningButton;
    
    public void Init()
    {
        weatherSlider.ValueChanged += value =>
        {
            Game.WeatherStrength = (float)value;
        };

        weatherSpeedSlider.ValueChanged += value =>
        {
            Game.WeatherSpeed = (float)value;
        };

        LightningButton.ButtonUp += () =>
        {
            Game.WeatherMgr.Lightning();
        };
    }
}