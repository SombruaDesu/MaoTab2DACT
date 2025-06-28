/*
 * @Author: MaoT
 * @Description: 音效播放器
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts.Component;

/// <summary>
/// 音频播放器
/// </summary> 
[GlobalClass]
public partial class AudioPlayer : AudioStreamPlayer
{
    /// <summary>
    /// 音频播放器类型
    /// </summary>
    public EAudioType AudioPlayerType = EAudioType.Sound; // 现阶段默认为音乐
    public void Init()
    {
        switch (AudioPlayerType)
        {
            case EAudioType.Sound:
                // 当游戏的音乐音量发生变化时
                Game.OnSoundVolumeChange += _ => 
                {
                    // 抽象的刷新属性 setter 的方法
                    Volume += 0;
                };
                break;
            case EAudioType.Music:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    /// <summary>
    /// 音频播放器音量
    /// </summary>
    public float Volume
    {
        // 由于没有分离音效缩放倍率，所以在这里得除回去得到绝对音量
        get => field / Game.SoundVolumeScale;
        set
        {
            field    = value * Game.SoundVolumeScale; // 乘以全局音效缩放倍率，得到绝对音量
            VolumeDb = Mathf.LinearToDb(field);  // Db和Volume（音量）是两个概念，使用数学库转一下
        }
    }
    
    /// <summary>
    /// 播放
    /// </summary>
    /// <param name="defVolume">默认音量</param>
    public void PlayAudio(float defVolume = 1)
    {
        if(IsPlaying()) return;
        
        Volume = defVolume;
        
        Play();
    }

    public async Task PlayAsyncAudio(float defVolume,CancellationTokenSource tokenSource)
    {
        PlayAudio(defVolume);
        
        try
        {
            // 持续循环，直到动画播放完毕或异步播放被取消
            while (!tokenSource.IsCancellationRequested &&
                   IsPlaying())
            {
                // 等待下一帧
                await Task.Delay(10, tokenSource.Token); // 使用取消令牌来支持任务取消
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <summary>
    /// 插值音量（线性）
    /// </summary>
    /// <param name="duration">插值时间</param>
    /// <param name="targetVolume">目标音量</param>
    public async Task LerpVolume(float duration, float targetVolume)
    {
        if(!IsPlaying()) return;
        
        float step = (targetVolume - Volume) / (duration * 60); // 每帧的音量增量（假设60帧每秒）
        
        while (Mathf.Abs(Volume - targetVolume) > 0.01f) // 直到接近目标音量
        {
            Volume += step;
            await Task.Delay(16); // 等待大约16毫秒（约60帧每秒）
        }

        Volume = targetVolume; // 确保最终音量为目标音量
    }
    
    /// <summary>
    /// 插值音量（先快后慢）
    /// </summary>
    /// <param name="duration">插值时间</param>
    /// <param name="targetVolume">目标音量</param>
    public async Task SmoothLerpVolume(float duration, float targetVolume)
    {
        if(!IsPlaying()) return;

        float startVolume = Volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += (float)Game.PhysicsDelta; // 增加经过的时间

            float t = elapsedTime / duration; // 计算插值比例

            t      = Mathf.SmoothStep(0f, 1f, t);              // 使用SmoothStep进行缓动
            Volume = Mathf.Lerp(startVolume, targetVolume, t); // 插值计算

            await Task.Delay(16); // 等待大约16毫秒
        }

        Volume = targetVolume; // 确保最终音量为目标音量
    }
}