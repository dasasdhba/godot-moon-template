using System.Collections.Generic;
using Godot;
using GodotTask;
using GodotTask.Triggers;

namespace Component;

public class AsyncLoader
{
    private Node Root { get ;set; }
    
    private PackedScene Scene { get ;set; }
    private string ScenePath { get ;set; }
    
    private int StackedCount { get ;set; }
    private bool Dead { get; set; } = false;
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1)
    {
        Root = root;
        Scene = scene;
        ScenePath = scene.ResourcePath;
        
        LoadedStackDict.TryAdd(ScenePath, new());
        
        AsyncInit(maxCount).Forget();
        
        Root.TreeExited += () => AsyncDead().Forget();
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
                GD.PushWarning($"AsyncLoader: All nodes are in use at {Root.Name} with {Scene.ResourcePath}. Consider increasing MaxCount.");
            #endif
                lock (InstantiateLock)
                {
                    return Scene.Instantiate();
                }
            }
            
            result = loaded.Stack.Pop();
            StackedCount--;
        }
        
        AddCreateTask().Forget();
        return result;
    }

    private async GDTaskVoid AddCreateTask(int count = 1)
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

    private async GDTaskVoid AsyncInit(int count)
    {
        await Root.OnReadyAsync();
        AddCreateTask(count).Forget();
    }

    private async GDTaskVoid AsyncDead()
    {
        await GDTask.RunOnThreadPool(() =>
        {
            var loaded = LoadedStackDict[ScenePath];
            lock (loaded.Lock)
            {
                Dead = true;

                for (int i = 0; i < StackedCount; i++)
                {
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
    private static readonly object InstantiateLock = new();
    
    private static readonly Dictionary<string, LoadedStack> LoadedStackDict = new();
    
    /// <summary>
    /// Should be called for first task run.
    /// </summary>
    private static async GDTaskVoid AsyncLoad()
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
                        if (loader.Dead && loader.StackedCount >= 0) continue;
                    }
                    
                    Node result;
                    lock (InstantiateLock)
                    {
                        result = loader.Scene.Instantiate();
                    }

                    lock (loaded.Lock)
                    {
                        if (loader.Dead && loader.StackedCount >= 0)
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
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1)
    {
        Loader = new(root, scene, maxCount);
    }
    
    public T Create() => (T)Loader.Create();
}