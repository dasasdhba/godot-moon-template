using System;
using System.Collections.Generic;
using Godot;
using GodotTask;

namespace Component;

public static class AsyncBackgroundLoader
{
    public struct AsyncTask
    {
        public PackedScene Scene { get ;set; }
        public Stack<Node> TargetStack { get ;set; }
        public object TargetLock { get ;set; }
        public Func<bool> IsDead { get ;set; }
    }
    
    public static Stack<AsyncTask> AsyncTasks = new();
    public static object TaskLock = new();
    public static object InstantiateLock = new();
    
    /// <summary>
    /// Should be called once from game singleton.
    /// </summary>
    public static async GDTaskVoid AsyncLoad()
    {
        await GDTask.RunOnThreadPool(() =>
        {
            while (true)
            {
                lock (TaskLock)
                {
                    if (AsyncTasks.Count == 0) continue;
                    
                    var task = AsyncTasks.Pop();
                    
                    lock (task.TargetLock)
                    {
                        if (task.IsDead()) continue;
                    }
                    
                    Node result;
                    lock (InstantiateLock)
                    {
                        result = task.Scene.Instantiate();
                    }

                    lock (task.TargetLock)
                    {
                        if (task.IsDead())
                        {
                            result.QueueFree();
                            continue;
                        }
                        task.TargetStack.Push(result);
                    }
                }
            }
        });
    }
}

public class AsyncLoader<T> where T : Node
{
    private Node Root { get ;set; }
    private PackedScene Scene { get ;set; }
    private int MaxCount { get ;set; }
    
    private bool Dead { get; set; } = false;
    
    private Stack<Node> LoadedNodes = new(); 
    private object StackLock = new();
    
    private AsyncBackgroundLoader.AsyncTask CreateTask;
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1)
    {
        Root = root;
        Scene = scene;
        MaxCount = maxCount;
        
        CreateTask = new()
        {
            Scene = scene,
            TargetStack = LoadedNodes,
            TargetLock = StackLock,
            IsDead = () => Dead
        };

        for (int i = 0; i < MaxCount; i++)
        {
            AddCreateTask();
        }
        
        Root.TreeExited += () =>
        {
            lock (StackLock)
            {
                Dead = true;

                foreach (var node in LoadedNodes)
                {
                    node.QueueFree();
                }
                LoadedNodes.Clear();
            }
        };
    }

    public T Create()
    {
        lock (StackLock)
        {
            if (LoadedNodes.Count == 0)
            {
            #if TOOLS
                GD.PushWarning($"AsyncLoader: All nodes are in use at {Root.Name} with {Scene.ResourcePath}. Consider increasing MaxCount.");
            #endif
                T result;
                lock (AsyncBackgroundLoader.InstantiateLock)
                {
                    result = Scene.Instantiate<T>();
                }
                return result;
            }
            
            var loaded = LoadedNodes.Pop();
            AddCreateTask();
            return (T)loaded;
        }
    }

    private void AddCreateTask()
    {
        lock (AsyncBackgroundLoader.TaskLock)
        {
            AsyncBackgroundLoader.AsyncTasks.Push(CreateTask);
        }
    }
}