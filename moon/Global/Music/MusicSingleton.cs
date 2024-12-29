using System;
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

        public double FadeTime { get; set; } = 1f;
        private double FadeTimer = 0f;
        
        public int FadeDir { get; set; } = 0;

        private float CachedVolume = 0f;
        private bool FadePaused = false;

        public void SetVolume(float volume, bool force = false)
        {
            Volume = volume;
            if (force || FadeDir == 0)
            {
                FadeDir = 0;
                CurrentVolume = volume;
            }
        }

        public void FadePlay(float pos = 0)
        {
            FadeIn();
            Play(pos);
        }

        public void DirectPlay(float pos = 0)
        {
            FadeReset();
            Play(pos);
        }

        public void FadeStop()
        {
            if (!Playing) return;

            FadeOut();
            FadePaused = false;
        }
        
        private void FadeIn()
        {
            FadeDir = 1;
            FadeTimer = 0d;
            CurrentVolume = 0f;
        }

        private void FadeOut()
        {
            FadeDir = -1;
            FadeTimer = 0d;
            CachedVolume = CurrentVolume;
        }

        private void FadeReset()
        {
            FadeTimer = 0d;
            FadeDir = 0;
            CurrentVolume = Volume;
        }
        
        public void Pause() => StreamPaused = true;
        public void Resume() => StreamPaused = false;
        public bool IsPaused() => StreamPaused;
        
        public void FadePause()
        {
            if (IsPaused()) return;
            
            FadeOut();
            FadePaused = true;
        }

        public void FadeResume()
        {
            if (!IsPaused()) return;
            
            FadeIn();
            Resume();
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
                
                var p = (float)Math.Clamp(FadeTimer / FadeTime, 0f, 1f);
                if (FadeDir == 1)
                    CurrentVolume = p * Volume;
                else
                    CurrentVolume = (1f - p) * CachedVolume;

                if (FadeTimer >= FadeTime)
                {
                    FadeTimer = 0f;
                    if (FadeDir == -1)
                    {
                        if (FadePaused) Pause();
                        else Stop();
                    }
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
        Players[channel].SetVolume(volume, force);
    }

    public void Play(AudioStream stream, int channel = 0, float volume = 1f)
    {
        Players[channel].Stream = stream;
        Players[channel].Volume = volume;
        Players[channel].DirectPlay();
    }

    public void Stop(int channel = 0) => Players[channel].Stop();

    public void StopAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            Stop(i);
    }

    public void Pause(int channel = 0) => Players[channel].Pause();
    public void Resume(int channel = 0) => Players[channel].Resume();
    public bool IsPaused(int channel = 0) => Players[channel].IsPaused();

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
    
    public void FadePause(int channel = 0) => Players[channel].FadePause();
    public void FadeResume(int channel = 0) => Players[channel].FadeResume();
    
    public void FadePauseAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            FadePause(i);
    }

    public void FadeResumeAll()
    {
        for (int i = 0; i < MaxChannel; i++)
            FadeResume(i);
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