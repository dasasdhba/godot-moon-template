using Global;
using Godot;

namespace Utils;

[GlobalClass]
public partial class MusicPlay : Node
{
    [ExportCategory("MusicPlay")]
    [Export]
    public AudioStream Stream { get; set; }

    public enum MusicSettingMode
    {
        Play,
        FadePlay,
        Stop,
        FadeStop
    }

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
                Apply();
            }
        };
    }

    public void Apply()
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

    public bool IsPlaying() => Moon.Music.IsPlaying(Stream, Channel);

    public void Play(bool reset, bool forceVolume = false)
    {
        if (!reset && IsPlaying())
            return;

        Moon.Music.SetVolume(Volume, Channel, forceVolume);
        Moon.Music.Play(Stream, Channel);
    }

    public void Play() => Play(false);

    public void Stop() => Moon.Music.Stop(Channel);

    public void FadePlay(bool reset, bool forceVolume = false)
    {
        if (!reset && IsPlaying())
            return;

        Moon.Music.SetVolume(Volume, Channel, forceVolume);
        Moon.Music.FadePlay(Stream, FadeTime, Channel);
    }

    public void FadePlay() => FadePlay(false);

    public void FadeStop() => Moon.Music.FadeStop(FadeTime, Channel);
}