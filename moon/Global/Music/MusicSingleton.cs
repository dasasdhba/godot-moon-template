using Godot;
using System.Collections.Generic;

namespace Global;

/// <summary>
/// Music Singleton
/// </summary>
public partial class MusicSingleton : Node
{
    public void PlayGlobal(string name)
        => GetNodeOrNull<AudioStreamPlayer>(name)?.Play();

    public static void Play(string name)
        => Moon.Music.PlayGlobal(name);

    /// <summary>
    /// Should not be altered in runtime.
    /// </summary>
    [ExportCategory("MusicSingleton")]
    [Export]
    public int MaxChannel { get; set; } = 8;

    public List<MusicPlayer> Players { get; set; } = new();

    /// <summary>
    /// AudioStreamPlayer with fade in/out support
    /// </summary>
    public partial class MusicPlayer : AudioStreamPlayer
    {
        /// <summary>
        /// liner volume
        /// </summary>
        public float Volume { get; set; } = 1f;

        /// <summary>
        /// Current linear volume
        /// </summary>
        public float CurrentVolume
        {
            get => Mathf.DbToLinear(VolumeDb);
            set => VolumeDb = Mathf.LinearToDb(value);
        }

        public float FadeTime { get; set; } = 1f;
        private double FadeTimer { get; set; } = 0f;
        
        public int FadeDir { get; set; } = 0;

        private float CachedVolume { get; set; } = 0f;

        public void FadeIn()
        {
            FadeDir = 1;
            FadeTimer = 0d;
            CurrentVolume = 0f;
        }

        public void FadePlay(float pos = 0)
        {
            FadeIn();
            Play(pos);
        }

        public void FadeStop()
        {
            if (!Playing) return;

            FadeDir = -1;
            FadeTimer = 0d;
            CachedVolume = CurrentVolume;
        }

        public void FadeReset()
        {
            FadeTimer = 0d;
            FadeDir = 0;
            CurrentVolume = Volume;
        }

        public override void _EnterTree()
        {
            CurrentVolume = Volume;
        }

        public override void _Process(double delta)
        {
            if (FadeDir != 0)
            {
                FadeTimer += delta;
                if (FadeDir == 1)
                    CurrentVolume = (float)(FadeTimer / FadeTime * Volume);
                else
                    CurrentVolume = (float)((1f - FadeTimer / FadeTime) * CachedVolume);

                if (FadeTimer >= FadeTime)
                {
                    FadeTimer = 0f;
                    if (FadeDir == -1)
                        Stop();
                    FadeDir = 0;
                    CurrentVolume = Volume;
                }
            }
        }
    }

    public override void _EnterTree()
    {
        GetTree().NodeAdded += (node) =>
        {
            if (node is AudioStreamPlayer player)
            {
                if (player.Bus == "Master")
                    player.Bus = "Sound";
            }
        };

        for (int i = 0; i < MaxChannel; i++)
        {
            MusicPlayer player = new()
            {
                Bus = "Music"
            };
            Players.Add(player);
            AddChild(player);
        }
    }

    public MusicPlayer GetMusicPlayer(int channel = 0) => Players[channel];

    public void SetVolume(float volume, int channel = 0, bool force = false)
    {
        Players[channel].Volume = volume;
        if (force || Players[channel].FadeDir == 0)
        {
            Players[channel].FadeDir = 0;
            Players[channel].CurrentVolume = volume;
        }
    }

    public void Play(AudioStream stream, int channel = 0, float volume = 1f)
    {
        Players[channel].Stream = stream;
        Players[channel].Volume = volume;
        Players[channel].FadeReset();
        Players[channel].Play();
    }

    public void Stop(int channel = 0) => Players[channel].Stop();

    public void StopAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            Stop(i);
    }

    public void Pause(int channel = 0) => Players[channel].StreamPaused = true;

    public void Resume(int channel = 0) => Players[channel].StreamPaused = false;

    public void PauseAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            Pause(i);
    }

    public void ResumeAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            Resume(i);
    }

    public void FadeIn(float fadetime = 1f, int channel = 0, float volume = 1f)
    {
        Players[channel].Volume = volume;
        Players[channel].FadeTime = fadetime;
        Players[channel].FadeIn();
    }

    public void FadePlay(AudioStream stream, float fadetime = 1f, int channel = 0, float volume = 1f)
    {
        Players[channel].Stream = stream;
        Players[channel].Volume = volume;
        Players[channel].FadeTime = fadetime;
        Players[channel].FadePlay();
    }

    public void FadeStop(float fadetime = 1f, int channel = 0)
    {
        Players[channel].FadeTime = fadetime;
        Players[channel].FadeStop();
    }

    public void FadeStopAll(float fadetime = 1f)
    {
        for (int i = 0; i < MaxChannel; i++)
            FadeStop(fadetime, i);
    }

    public bool IsPlaying(int channel = 0, bool checkFade = true)
        => Players[channel].Playing && (!checkFade || Players[channel].FadeDir != -1);

    public bool IsPlaying(AudioStream stream, int channel = 0, bool checkFade = true)
        => Players[channel].Stream == stream && IsPlaying(channel, checkFade);
}