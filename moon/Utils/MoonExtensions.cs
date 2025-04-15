using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using Component;
using Global;
using GodotTask;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Utils;

// useful node extension functions

public static class MoonExtensions
{
    #region Collections

    public static void AddNode<T>(this ICollection<T> arr, T node) where T : Node
    {
        node.TreeExited += () => arr.Remove(node);
        arr.Add(node);
    }

    #endregion
    
    #region DirAccess

    public static IEnumerable<string> GetFilePaths(this DirAccess dir, Func<string, bool> filter = null)
    {
        var root = dir.GetCurrentDir();
        foreach (var file in dir.GetFiles())
        {
            if (filter != null && !filter(file)) continue;
            yield return root + "/" + file;
        }
    }
    
    public static IEnumerable<string> GetFilePathsRecursively(this DirAccess dir, Func<string, bool> filter = null)
    {
        var root = dir.GetCurrentDir();
        foreach (var file in dir.GetFilePaths(filter)) yield return file;
        foreach (var sub in dir.GetDirectories())
        {
            var subDir = DirAccess.Open(root + "/" + sub);
            foreach (var file in subDir.GetFilePathsRecursively(filter)) yield return file;
        }
    }
    
    #endregion
    
    #region PackedScene
    
    private static object _instanceLock = new();

    public static Node InstantiateSafely(this PackedScene scene)
    {
        lock (_instanceLock)
        {
            return scene.Instantiate();
        }
    }
    
    public static T InstantiateSafely<T>(this PackedScene scene) where T : Node
    {
        lock (_instanceLock)
        {
            return scene.Instantiate<T>();
        }
    }
    
    public static IEnumerable<Node> InstantiateSafely(this PackedScene scene, int count)
    {
        lock (_instanceLock)
        {
            for (int i = 0; i < count; i++)
            {
                yield return scene.Instantiate();
            }
        }
    }
    
    public static IEnumerable<T> InstantiateSafely<T>(this PackedScene scene, int count) where T : Node
    {
        lock (_instanceLock)
        {
            for (int i = 0; i < count; i++)
            {
                yield return scene.Instantiate<T>();
            }
        }
    }
    
    #endregion

    #region ConfigFile

    public static System.Collections.Generic.Dictionary<string, Variant> GetSection(this ConfigFile config, string section)
    {
        var result = new System.Collections.Generic.Dictionary<string, Variant>();
        foreach (var key in config.GetSectionKeys(section))
        {
            result[key] = config.GetValue(section, key);
        }
        return result;
    }

    public static void SetSection(this ConfigFile config, string section, System.Collections.Generic.Dictionary<string, Variant> values)
    {
        foreach (var key in values.Keys)
        {
            config.SetValue(section, key, values[key]);
        }
    }

    #endregion

    #region Node
    
    public static string GetUniquePath(this Node node)
        => Moon.Scene.MainViewport.GetPathTo(node);

    public static Tween CreatePhysicsTween(this Node node)
    {
        var tween = node.CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Physics);
        return tween;
    }
    
    public static T FindParent<T>(this Node node, Func<T, bool> filter = null) where T : Node
    {
        var p = node.GetParent();
        while (p != null)
        {
            if (p is T t && (filter == null || filter(t))) return t;
            p = p.GetParent();
        }
        
        return null;
    }
    
    /// <summary>
    /// Bind internal node with parent. This prevents duplicate issues.
    /// </summary>
    public static void BindParent(this Node node, Node parent)
    {
        node.TreeEntered += () =>
        {
            if (node.GetParent() != parent)
                node.QueueFree();
        };
        
        // this conflicts with object pooling
        // though the performance issue may not be very serious
        
        parent.TreeExited += node.QueueFree;
    }

    public static IEnumerable<T> GetChildren<T>(this Node node, 
        bool includeInternal = false) where T : Node
    {
        foreach (var child in node.GetChildren(includeInternal))
        {
            if (child is T t) yield return t;
        }
    }

    public static IEnumerable<Node> GetChildrenRecursively(this Node node, bool includeInternal = false)
    {
        foreach (var child in node.GetChildren(includeInternal))
        {
            yield return child;
            foreach (var c in child.GetChildrenRecursively(includeInternal))
            {
                yield return c;
            }
        }
    }
    
    public static IEnumerable<T> GetChildrenRecursively<T>(this Node node, 
        bool includeInternal = false) where T : Node
    {
        foreach (var child in node.GetChildrenRecursively(includeInternal))
        {
            if (child is T t) yield return t;
        }
    }

    public static void SetChildrenRecursively(this Node node, Action<Node> action, bool includeInternal = false)
    {
        foreach (var child in node.GetChildren(includeInternal))
        {
            action?.Invoke(child);
            SetChildrenRecursively(child, action, includeInternal);
        }
    }
    
    private const string ChildrenCacheTag = "MCCache";
    private const string ChildrenRecursivelyCacheTag = "MCRCache";
    private static void ClearChildrenCache(this GodotObject node, string tag)
    {
        foreach (string meta in node.GetMetaList())
        {
            if (meta.StartsWith(tag))
            {
                node.RemoveMeta(meta);
            }
        }
    }

    private static void SetChildrenCacheMonitor(this Node node, Node target, string tag)
    {
        var signalTag = $"{tag}_MSignal";
        if (target.HasData(signalTag))
        {
            var arr = target.GetData<Godot.Collections.Array<Node>>(signalTag);
            if (arr.Contains(node)) return;
            arr.Add(node);
        }
        else
        {
            target.SetData(signalTag, 
            new Godot.Collections.Array<Node> { node });
        }
        
        target.ChildEnteredTree += c => ClearChildrenCache(node, tag);
        target.ChildExitingTree += c => ClearChildrenCache(node, tag);
    }

    public static IEnumerable<T> GetChildrenCached<[MustBeVariant] T>(this Node node, 
        string tag = "Default", bool includeInternal = false) where T : Node
    {
        if (node.HasData($"{ChildrenCacheTag}{tag}"))
        {
            return node.GetData<Godot.Collections.Array<T>>($"{ChildrenCacheTag}{tag}");
        }
        
        Godot.Collections.Array<T> result = new(node.GetChildren<T>(includeInternal));
        node.SetData($"{ChildrenCacheTag}{tag}", result);
        node.SetChildrenCacheMonitor(node, ChildrenCacheTag);
        return result;
    }

    public static IEnumerable<T> GetChildrenRecursivelyCached<[MustBeVariant] T>(this Node node,
        string tag = "Default", bool includeInternal = false) where T : Node
    {
        if (node.HasData($"{ChildrenRecursivelyCacheTag}{tag}"))
        {
            return node.GetData<Godot.Collections.Array<T>>($"{ChildrenRecursivelyCacheTag}{tag}");
        }
        
        Godot.Collections.Array<T> result = [];
        foreach (var child in node.GetChildrenRecursively(includeInternal))
        {
            if (child is T t) result.Add(t);
            node.SetChildrenCacheMonitor(child, ChildrenRecursivelyCacheTag);
        }
        node.SetData($"{ChildrenRecursivelyCacheTag}{tag}", result);
        node.SetChildrenCacheMonitor(node, ChildrenRecursivelyCacheTag);
        return result;
    }

    /// <summary>
    /// if node is in pool, remove it from parent instead.
    /// </summary>
    public static void TryQueueFree(this Node node)
    {
        if (NodePool.IsInPool(node))
        {
            node.GetParent().CallDeferred(Node.MethodName.RemoveChild, node);
            node.Connect(Node.SignalName.TreeExited, Callable.From(() =>
            {
                NodePool.GetPool(node).ReturnPool(node);
            }), (int)GodotObject.ConnectFlags.OneShot);
            return;
        }
        
        node.QueueFree();
    }

    /// <summary>
    /// Flip PlatformerMove2D or SpriteDir when init
    /// </summary>
    public static void TryInitFlip(this Node node)
    {
        if (node is IFlipInit flip) flip.FlipInit();
    }
    
    /// <summary>
    /// Flip All possible nodes when init
    /// </summary>
    /// <param name="node"></param>
    public static void TryInitFlipAll(this Node node)
    {
        node.TryInitFlip();
        node.SetChildrenRecursively(TryInitFlip);
    }

    public static bool TryGetFlipH(this Node node)
    {
        var flip = node.Get(Sprite2D.PropertyName.FlipH);
        if (flip.VariantType != Variant.Type.Nil)
        {
            return flip.AsBool();
        }
        flip = node.Get(AnimGroup2D.PropertyName.FlipH);
        if (flip.VariantType != Variant.Type.Nil)
        {
            return flip.AsBool();
        }
        
        return false;
    }
    
    public static void TrySetFlipH(this Node node, bool value)
    {
        var flip = node.Get(Sprite2D.PropertyName.FlipH);
        if (flip.VariantType != Variant.Type.Nil)
        {
            node.Set(Sprite2D.PropertyName.FlipH, value);
        }
        flip = node.Get(AnimGroup2D.PropertyName.FlipH);
        if (flip.VariantType != Variant.Type.Nil)
        {
            node.Set(AnimGroup2D.PropertyName.FlipH, value);
        }
    }
    
    #endregion
    
    #region CanvasItem

    /// <summary>
    /// Useful to draw atlas texture with tiled mode
    /// </summary>
    public static void DrawTextureRectTiled(this CanvasItem item, Texture2D texture,
        Rect2 rect, Color? modulate = null)
    {
        var size = texture.GetSize();
        item.DrawTextureRectRegionTiled(texture, rect, 
            new(Vector2.Zero, size), modulate);
    }
    
    /// <summary>
    /// Useful to draw atlas texture with tiled mode
    /// </summary>
    public static void DrawTextureRectRegionTiled(this CanvasItem item, Texture2D texture,
        Rect2 rect, Rect2 srcRect, Color? modulate = null)
    {
        var flipH = rect.Size.X * srcRect.Size.X < 0f;
        var flipV = rect.Size.Y * srcRect.Size.Y < 0f;
        rect = new Rect2(rect.Position, rect.Size.Abs());
        srcRect = new Rect2(srcRect.Position, srcRect.Size.Abs());
        
        var rx = rect.Size.X;
        var ry = rect.Size.Y;
        var ux = srcRect.Size.X;
        var uy = srcRect.Size.Y;
        if (ux <= 0f || uy <= 0f) return;
        
        var px = 0f;
        while (px < rx)
        {
            var py = 0f;
            while (py < ry)
            {
                var w = Math.Min(ux, rx - px);
                var h = Math.Min(uy, ry - py);
                var x = flipH ? ux - w : 0f;
                var y = flipV ? uy - h : 0f;
                var sRect = new Rect2(x, y ,w ,h);
                if (flipH) w *= -1f;
                if (flipV) h *= -1f;
                var rRect = new Rect2(rect.Position + new Vector2(px, py), w, h);
                item.DrawTextureRectRegion(texture, rRect, sRect, modulate);
                py += uy;
            }
            px += ux;
        }
    }

    public static void SetShaderParam(this CanvasItem item, string param, Variant value)
    {
        if (item.Material is not ShaderMaterial shader) return;
        shader.SetShaderParameter(param, value);
    }

    public static T GetShaderParam<[MustBeVariant] T>(this CanvasItem item, string param)
    {
        if (item.Material is not ShaderMaterial shader) return default;
        return shader.GetShaderParameter(param) is T t ? t : default;
    }
    
    private const string RecorderTag = "_CanvasItemRecorder";

    public static void AddRecorder(this CanvasItem item)
    {
        if (item.HasData(RecorderTag)) return;
        var recorder = new MotionRecorder2D() { Target = item };
        if (item.IsNodeReady())
            item.AddChild(recorder);
        else
            item.CallDeferred(Node.MethodName.AddChild, recorder);
        item.SetData(RecorderTag, recorder);
    }

    public static MotionRecorder2D GetRecorder(this CanvasItem item)
    {
        if (!item.HasData(RecorderTag)) item.AddRecorder();
        return item.GetData<MotionRecorder2D>(RecorderTag);
    }
    
    #endregion
    
    #region TileMap

    public static bool HasLayer(this TileMap tilemap, string layer)
    {
        for (int i = 0; i < tilemap.GetLayersCount(); i++)
        {
            if (tilemap.GetLayerName(i) == layer) return true;
        }
        
        return false;
    }
    
    public static int GetLayerIndex(this TileMap tilemap, string layer)
    {
        for (int i = 0; i < tilemap.GetLayersCount(); i++)
        {
            if (tilemap.GetLayerName(i) == layer) return i;
        }
        
        return -1;
    }

    #endregion
    
    #region PhysicsBody
    
    public static bool IsOverlapping(this PhysicsBody2D body, Vector2 offset = default)
        => body.TestMove(
            body.GlobalTransform with { Origin = body.GlobalPosition + offset },
            Vector2.Zero
        );
        
    public static bool IsOverlapping(this PhysicsBody3D body, Vector3 offset = default)
        => body.TestMove(
            body.GlobalTransform with { Origin = body.GlobalPosition + offset },
            Vector3.Zero
        );

    public static bool TryPushOut(this PhysicsBody2D body, Vector2 motion)
    {
        if (body.IsOverlapping(motion)) return false;
        
        body.GlobalPosition += motion;
        body.MoveAndCollide(-motion);

        return true;
    }
    
    public static bool TryPushOut(this PhysicsBody3D body, Vector3 motion)
    {
        if (body.IsOverlapping(motion)) return false;
        
        body.GlobalPosition += motion;
        body.MoveAndCollide(-motion);

        return true;
    }

    private static async GDTask GetThroughAsync(this PhysicsBody2D body, CancellationToken ct)
    {
        var origin = body.CollisionMask;
        body.CollisionMask = 0;
        
        bool isOverlapping()
        {
            body.CollisionMask = origin;
            var result = body.IsOverlapping();
            body.CollisionMask = 0;
            return result;
        }
        
        await Async.WaitUntilPhysics(body, () => isOverlapping(), ct);
        await Async.WaitUntilPhysics(body, () => !isOverlapping(), ct);
        
        body.CollisionMask = origin;
    }

    public static CTask GetThrough(this PhysicsBody2D body)
    {
        var origin = body.CollisionMask;
        return new(body.GetThroughAsync)
        {
            OnCancel = () => body.CollisionMask = origin
        };
    }
    
    private static async GDTask GetThroughAsync(this PhysicsBody3D body, CancellationToken ct)
    {
        var origin = body.CollisionMask;
        body.CollisionMask = 0;
        
        bool isOverlapping()
        {
            body.CollisionMask = origin;
            var result = body.IsOverlapping();
            body.CollisionMask = 0;
            return result;
        }
        
        await Async.WaitUntilPhysics(body, () => isOverlapping(), ct);
        await Async.WaitUntilPhysics(body, () => !isOverlapping(), ct);
        
        body.CollisionMask = origin;
    }

    public static CTask GetThrough(this PhysicsBody3D body)
    {
        var origin = body.CollisionMask;
        return new(body.GetThroughAsync)
        {
            OnCancel = () => body.CollisionMask = origin
        };
    }
    
    #endregion
}