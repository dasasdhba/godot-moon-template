using System;
using System.Collections.Generic;
using Global;
using Godot;
using GodotTask;
using GodotTask.Triggers;
using Utils;

namespace Component;

public class AsyncLoader
{
    private Node Root { get ;set; }
    
    private PackedScene Scene { get ;set; }
    private string ScenePath { get ;set; }
    
    private int StackedCount { get ;set; }
    private bool Dead { get; set; } = false;
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1, int bufferCount = 0)
    {
        Scene = scene;
        InitWithPath(root, scene.ResourcePath, maxCount, bufferCount);
    }

    public AsyncLoader(Node root, string scenePath, int maxCount = 1, int bufferCount = 0)
    {
        InitWithPath(root, scenePath, maxCount, bufferCount);
    }

    private void InitWithPath(Node root, string scenePath, int maxCount = 1, int bufferCount = 0)
    {
        Root = root;
        ScenePath = scenePath;
        
        LoadedStackDict.TryAdd(ScenePath, new());
        
        if (bufferCount > 0) Init(bufferCount);
        var asyncCount = maxCount - bufferCount;
        if (asyncCount > 0)
            AsyncInit(asyncCount).Forget();
        
        Root.Connect(Node.SignalName.TreeExited, Callable.From(() =>
        {
            AsyncDead().Forget();
        }), (uint)GodotObject.ConnectFlags.OneShot);
    }

    public Node Create()
    {
        Node result;
        var loaded = LoadedStackDict[ScenePath];
        lock (loaded.Lock)
        {
            if (loaded.Stack.Count == 0)
            {
            #if TOOLS
                GD.PushWarning($"AsyncLoader: All nodes are in use at {Root.GetUniquePath()} with {Scene.ResourcePath}. Consider increasing MaxCount, or create buffer at start.");
            #endif
                return Scene.InstantiateSafely();
            }
            
            result = loaded.Stack.Pop();
            StackedCount--;
        }
        
        AddCreateTask().Forget();
        return result;
    }

    private async GDTask AddCreateTask(int count = 1)
    {
        await GDTask.RunOnThreadPool(() =>
        {
            var run = false;
            
            lock (TaskLock)
            {
                if (AsyncTasks.Count == 0) run = true;
                
                for (int i = 0; i < count; i++)
                {
                    AsyncTasks.Enqueue(this);
                }
            }
            
            if (run) AsyncLoad().Forget();
        });
    }

    private void Init(int count)
    {
        Scene ??= Moon.LoadSafely<PackedScene>(ScenePath);
        var stack = LoadedStackDict[ScenePath];
        foreach (var r in Scene.InstantiateSafely(count))
        {
            stack.Stack.Push(r);
            StackedCount++;
        }
    }

    private async GDTask AsyncInit(int count)
    {
        await Root.OnReadyAsync();
        Scene ??= await Moon.LoadAsync<PackedScene>(ScenePath);
        AddCreateTask(count).Forget();
    }

    private async GDTask AsyncDead()
    {
        await GDTask.RunOnThreadPool(() =>
        {
            var loaded = LoadedStackDict[ScenePath];
            lock (loaded.Lock)
            {
                Dead = true;

                for (int i = 0; i < StackedCount; i++)
                {
                    if (loaded.Stack.Count == 0) break;
                    var result = loaded.Stack.Pop();
                    result.QueueFree();
                }
            }
        });
    }
    
    // static background loader
    
    private struct LoadedStack
    {
        public Stack<Node> Stack { get ;set; } = new();
        public object Lock { get ;set; } = new();
        
        public LoadedStack() {}
    }
    
    private static readonly Queue<AsyncLoader> AsyncTasks = new();
    private static readonly object TaskLock = new();
    
    private static readonly Dictionary<string, LoadedStack> LoadedStackDict = new();
    
    /// <summary>
    /// Should be called for first task run.
    /// </summary>
    private static async GDTask AsyncLoad()
    {
        await GDTask.RunOnThreadPool(() =>
        {
            while (true)
            {
                lock (TaskLock)
                {
                    if (AsyncTasks.Count == 0) return;
                    
                    var loader = AsyncTasks.Dequeue();
                    var loaded = LoadedStackDict[loader.ScenePath];
                    
                    lock (loaded.Lock)
                    {
                        if (loader.Dead) continue;
                    }
                    
                    var result = loader.Scene.InstantiateSafely();

                    lock (loaded.Lock)
                    {
                        if (loader.Dead)
                        {
                            result.QueueFree();
                            continue;
                        }
                        
                        loaded.Stack.Push(result);
                        loader.StackedCount++;
                    }
                    
                    if (AsyncTasks.Count == 0) return;
                }
            }
        });
    }
}

public class AsyncLoader<T> where T : Node
{
    private AsyncLoader Loader { get ;set; }
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1, int bufferCount = 0)
    {
        Loader = new(root, scene, maxCount, bufferCount);
    }
    
    public AsyncLoader(Node root, string scenePath, int maxCount = 1, int bufferCount = 0)
    {
        Loader = new(root, scenePath, maxCount, bufferCount);
    }

    public T Create()
    {
        var result = Loader.Create();
    #if TOOLS
        if (result is not T)
        {
            GD.PushError($"AsyncLoader<{typeof(T)}> created a node of type {result.GetType()}, which is not desired.");
            return null;
        }
    #endif
    
        return (T)result;
    }
}