/*
 * @Author: MaoT
 * @Description: 游戏管理器，音效部分
 */

using System;

namespace MaoTab.Scripts;

public static partial class Game
{
    public static float AllVolumeScale;
    
    /// <summary>
    /// 游戏音乐音量
    /// </summary>
    public static float SoundVolumeScale 
    {
        get;
        set
        {
            field = value;
            OnSoundVolumeChange?.Invoke(value);
        }
    }
    
    public static event Action<float> OnSoundVolumeChange;
    
    
}