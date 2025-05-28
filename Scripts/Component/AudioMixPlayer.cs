/*
 * @Author: MaoT
 * @Description: 音频混合播放器
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts.Component;

[GlobalClass]
public partial class AudioMixPlayer : Node
{
    /// <summary>
    /// 播放器组
    /// </summary>
    public List<AudioPlayer> Players = [];

    /// <summary>
    /// 默认运行的音效播放器
    /// </summary>
    public int DefaultAudioPlayer = 0;

    /// <summary>
    /// 激活的播放器索引
    /// </summary>
    public int ActiveAudioPlayer = 0;

    public bool IsPlaying;

    /// <summary>
    /// 是否在切换音效播放器中
    /// </summary>
    public bool Switching { private set; get; }

    public void Init()
    {
        foreach (Node child in GetChildren())
        {
            if (child is AudioPlayer audioPlayer)
            {
                Players.Add(audioPlayer);
            }
        }
    }

    public void Play()
    {
        if (IsPlaying) return;
        IsPlaying = true;

        // 播放组内所有音效播放器，仅默认音效播放器有音量
        // 此方法并不是并发，可能存在略微延迟
        for (var index = 0; index < Players.Count; index++)
        {
            var player = Players[index];

            if (index == DefaultAudioPlayer)
                player.PlayAudio(1);
            else
                player.PlayAudio(0);

            ActiveAudioPlayer = DefaultAudioPlayer;
        }
    }

    /// <summary>
    /// 切换播放器
    /// </summary>
    /// <param name="playerIndex">播放器索引</param>
    /// <param name="duration">切换过渡时间</param>
    private async Task ChangePlayer(int playerIndex,float duration)
    {
        if (!IsPlaying) return;

        if (playerIndex >= Players.Count || playerIndex == ActiveAudioPlayer) return;

        if (Switching)
        {
            return;
        }

        Switching = true;

        // 启动两个音量插值任务
        var fadeOutTask = Players[ActiveAudioPlayer].SmoothLerpVolume(duration, 0);
        var fadeInTask  = Players[playerIndex].SmoothLerpVolume(duration, 1);

        await Task.WhenAll(fadeOutTask, fadeInTask);

        Switching = false;

        ActiveAudioPlayer = playerIndex;
    }

    public void SwitchPlayer(float duration = 3f)
    {
        if (!IsPlaying || Switching) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (i != ActiveAudioPlayer)
            {
                _ = ChangePlayer(i,duration);
                break;
            }
        }
    }

    public void SwitchPlayerTo(int index,float duration = 3f)
    {
        if (!IsPlaying || Switching || index == ActiveAudioPlayer) return;

        _ = ChangePlayer(index,duration);
    }
}