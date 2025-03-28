﻿using System.Threading;
using Godot;
using GodotTask;

namespace Global;

public partial class Moon : Node
{
    public Moon() : base()
    {
        Singleton = this;
        
        TreeEntered += () =>
        {
            Save = GetNode<SaveSingleton>("Save");
            Music = GetNode<MusicSingleton>("Music");
            Scene = GetNode<SceneSingleton>("Scene");
        };
    }
    
    public static Moon Singleton { get ;private set; }
    public static SaveSingleton Save { get; private set; }
    public static MusicSingleton Music { get; private set; }
    public static SceneSingleton Scene { get; private set; }
    
    private static object _ResourceLock = new();
    public static T LoadSafely<T>(string path) where T : Resource
    {
        lock (_ResourceLock)
        {
            return GD.Load<T>(path);
        }
    }

    public static async GDTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Resource
    {
        T result = null;

        await GDTask.RunOnThreadPool(() =>
        {
            result = LoadSafely<T>(path);
        }, ct);
        
        return result;
    }
}