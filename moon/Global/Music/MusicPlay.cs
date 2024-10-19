﻿using Global;
using Godot;
using Godot.Collections;

namespace Utils;

[GlobalClass]
public partial class MusicPlay : Node
{
    [ExportCategory("MusicPlay")]
    [Export]
    public AudioStream Stream { get; set; }

    public enum MusicSettingMode
    { Play, FadePlay, Stop, FadeStop }

    [Export]
    public MusicSettingMode SettingMode { get; set; } = MusicSettingMode.Play;

    [Export]
    public int Channel { get; set; } = 0;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Volume { get; set; } = 1f;

    [Export]
    public float FadeTime { get; set; } = 1f;

    [Export]
    public bool Autoplay { get; set; } = true;

    public MusicPlay() : base()
    {
        Ready += () =>
        {
            if (Autoplay)
            {
                switch (SettingMode)
                {
                    case MusicSettingMode.Play:
                        Play();
                        break;

                    case MusicSettingMode.FadePlay:
                        FadePlay();
                        break;

                    case MusicSettingMode.Stop:
                        Stop();
                        break;

                    case MusicSettingMode.FadeStop:
                        FadeStop();
                        break;
                }
            }
        };
    }

    public bool IsPlaying() => Singleton.Music.IsPlaying(Stream, Channel);

    public void Play(bool reset, bool forceVolume = false)
    {
        if (!reset && IsPlaying())
            return;

        Singleton.Music.SetVolume(Volume, Channel, forceVolume);
        Singleton.Music.Play(Stream, Channel);
    }

    public void Play() => Play(false);

    public void Stop() => Singleton.Music.Stop(Channel);

    public void FadePlay(bool reset, bool forceVolume = false)
    {
        if (!reset && IsPlaying())
            return;

        Singleton.Music.SetVolume(Volume, Channel, forceVolume);
        Singleton.Music.FadePlay(Stream, FadeTime, Channel);
    }

    public void FadePlay() => FadePlay(false);

    public void FadeStop() => Singleton.Music.FadeStop(FadeTime, Channel);
}