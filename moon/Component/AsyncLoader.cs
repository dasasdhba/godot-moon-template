using System.Collections.Generic;
using Godot;
using GodotTask;

namespace Component;

public class AsyncLoader<T> where T : Node
{
    public static async GDTask<T> Load(PackedScene scene)
    {
        return await GDTask.RunOnThreadPool(() => scene.Instantiate<T>());
    }
    
    public static async GDTask<T> Load(string scenePath)
    {
        return await GDTask.RunOnThreadPool(() =>
        {
            var scene = GD.Load<PackedScene>(scenePath);
            return scene.Instantiate<T>();
        });
    }
    
    protected Node Root { get ;set; }
    protected PackedScene Scene { get ;set; }
    protected int MaxCount { get ;set; }
    
    protected bool Dead { get; set; } = false;
    
    protected Stack<T> LoadedNodes = new(); 
    private object StackLock = new();
    
    public AsyncLoader(Node root, PackedScene scene, int maxCount = 1)
    {
        Root = root;
        Scene = scene;
        MaxCount = maxCount;

        for (int i = 0; i < MaxCount; i++)
        {
            AsyncLoad().Forget();
        }
        
        Root.TreeExited += () =>
        {
            Dead = true;

            lock (StackLock)
            {
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
                return Scene.Instantiate<T>();
            }
            
            var result = LoadedNodes.Pop();
            AsyncLoad().Forget();
            return result;
        }
    }

    protected async GDTaskVoid AsyncLoad()
    {
        if (Dead) return;
        
        var node = await Load(Scene);
            
        if (Dead)
        {
            node.QueueFree();
            return;
        }
            
        lock (StackLock)
        {
            if (Dead)
            {
                node.QueueFree();
                return;
            }
            
            LoadedNodes.Push(node);
        }
    }
}