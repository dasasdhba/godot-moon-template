using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using Component;
using GodotTask;

namespace Utils;

// useful node extension functions

public static class NodeExtensions
{
    #region Node
    
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
        
        // HACK: this conflicts with object pooling
        // though the performance issue may not be very serious
        
        parent.TreeExited += node.QueueFree;
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

    public static void SetChildrenRecursively(this Node node, Action<Node> action, bool includeInternal = false)
    {
        foreach (var child in node.GetChildren(includeInternal))
        {
            action?.Invoke(child);
            SetChildrenRecursively(child, action, includeInternal);
        }
    }
    
    /// <summary>
    /// if node is in pool, remove it from parent instead.
    /// </summary>
    public static void TryQueueFree(this Node node)
    {
        if (NodePool.IsInPool(node))
        {
            node.GetParent().CallDeferred(Node.MethodName.RemoveChild, node);
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
        
        await Async.DelegatePhysics(body, () => isOverlapping(), ct);
        await Async.DelegatePhysics(body, () => !isOverlapping(), ct);
        
        body.CollisionMask = origin;
    }

    public static TaskCanceller GetThrough(this PhysicsBody2D body)
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
        
        await Async.DelegatePhysics(body, () => isOverlapping(), ct);
        await Async.DelegatePhysics(body, () => !isOverlapping(), ct);
        
        body.CollisionMask = origin;
    }

    public static TaskCanceller GetThrough(this PhysicsBody3D body)
    {
        var origin = body.CollisionMask;
        return new(body.GetThroughAsync)
        {
            OnCancel = () => body.CollisionMask = origin
        };
    }
    
    #endregion
}