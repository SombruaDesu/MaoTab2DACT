/*
 * @Author: MaoT
 * @Description: 玩家对象，特效部分
 */

using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    [Export] GpuParticles2D RainHitParticles;

    private int preWeatherStrength;
    
    public void FxUpdate()
    {
        Mathf.Lerp(RainHitParticles.Amount, Game.WeatherStrength * 8,1);
    }
}