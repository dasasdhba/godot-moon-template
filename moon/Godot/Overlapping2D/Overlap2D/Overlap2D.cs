using System;
using System.Collections.Generic;

namespace Godot;

/// <summary>
/// Base class of overlapping system.
/// Do overlapping query through Godot Physics Server 2D.
/// </summary>
public partial class Overlap2D
{
    public PhysicsShapeQueryParameters2D QueryParameters { get; set; } = new() 
    { 
        CollisionMask = 1,
        Exclude = new()
    };
    public int MaxResults { get; set; } = 32;

    public bool CollideWithAreas
    {
        get => QueryParameters.CollideWithAreas;
        set => QueryParameters.CollideWithAreas = value;
    }

    public bool CollideWithBodies
    {
        get => QueryParameters.CollideWithBodies;
        set => QueryParameters.CollideWithBodies = value;
    }

    public float Margin
    {
        get => QueryParameters.Margin;
        set => QueryParameters.Margin = value;
    }

    public uint CollisionMask
    {
        get => QueryParameters.CollisionMask;
        set => QueryParameters.CollisionMask = value;
    }

    // space
    protected Rid Space { get ;set; }
    protected PhysicsDirectSpaceState2D SpaceState { get ;set ;}

    public Overlap2D() { }

    /// <summary>
    /// Construct with rid.
    /// </summary>
    /// <param name="space">The RID of space.</param>
    public Overlap2D(Rid space) => SetSpace(space);

    /// <summary>
    /// Construct with node, using the node's space as the physics space.
    /// Should be called at least in <c>Node._EnterTree()</c>,
    /// or <c>Node.GetViewport()</c> will return null.
    /// </summary>
    /// <param name="node">The node to query space.</param>
    public Overlap2D(Node node) => SetSpace(node);

    /// <summary>
    /// Change space.
    /// </summary>
    /// <param name="space">The RID of space.</param>
    public void SetSpace(Rid space)
    {
        Space = space;
        SpaceState = PhysicsServer2D.SpaceGetDirectState(Space);
    }

    /// <summary>
    /// Using the node's space as the physics space.
    /// Should be called at least in <c>Node._EnterTree()</c>,
    /// or <c>Node.GetViewport()</c> will return null.
    /// </summary>
    /// <param name="node">The node to query space.</param>
    public void SetSpace(Node node)
    {
        Rid space;
        if (node is Area2D area)
        {
            space = PhysicsServer2D.AreaGetSpace(area.GetRid());
        }
        else if (node is PhysicsBody2D body)
        {
            space = PhysicsServer2D.BodyGetSpace(body.GetRid());
        }
        else
        {
            space = node.GetViewport().FindWorld2D().Space;
        }

        SetSpace(space);
    }

    public bool GetCollisionMaskValue(int layer)
        => ((CollisionMask >> (layer - 1)) & 1) == 1;

    public void SetCollisionMaskValue(int layer, bool value)
    {
        int n = layer - 1;
        int mask = (int)CollisionMask;
        if (value) mask |= 1 << n;
        else mask &= ~(1 << n);
        CollisionMask = (uint)mask;
    }

    // exception
    public void ClearException() => QueryParameters.Exclude = new();

    public void AddException(Rid rid)
    {
        var arr = QueryParameters.Exclude;
        arr.Add(rid);
        QueryParameters.Exclude = arr;
    }

    public void AddException(CollisionObject2D obj)
    {
        AddException(obj.GetRid());
    }

    public void RemoveException(Rid rid)
    {
        var arr = QueryParameters.Exclude;
        arr.Remove(rid);
        QueryParameters.Exclude = arr;
    }
    public void RemoveException(CollisionObject2D obj) => RemoveException(obj.GetRid());
    
    public IEnumerable<OverlapResult2D<T>> QueryOverlappingObjects<T>(Func<OverlapResult2D<T>, bool> filter, bool excludeOthers = false) where T : GodotObject
    {
        if (!PhysicsServer2D.SpaceIsActive(Space)) yield break;

        var query = SpaceState.IntersectShape(QueryParameters, MaxResults);
        for (int i = 0; i < query.GetCollisionCount(); i++)
        {
            var col = query.GetCollider(i);
            if (col is T colt)
            {
                var result = new OverlapResult2D<T>()
                {
                    Collider = colt,
                    Rid = query.GetRid(i),
                };

                if (filter == null || filter(result))
                {
                    yield return result;
                    continue;
                }
            }
            
            if (excludeOthers)
            {
                AddException(query.GetRid(i));
            }
        }
    }
    
    public IEnumerable<OverlapResult2D<T>> QueryOverlappingObjects<T>(bool excludeOthers = false) where T : GodotObject
        => QueryOverlappingObjects<T>(null, excludeOthers);
    public IEnumerable<OverlapResult2D<GodotObject>> QueryOverlappingObjects(Func<OverlapResult2D<GodotObject>, bool> filter, bool excludeOthers = false)
        => QueryOverlappingObjects<GodotObject>(filter, excludeOthers);
    public IEnumerable<OverlapResult2D<GodotObject>> QueryOverlappingObjects()
        => QueryOverlappingObjects(null);
}