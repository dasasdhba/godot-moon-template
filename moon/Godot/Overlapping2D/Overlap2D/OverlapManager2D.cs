using System;
using System.Collections.Generic;
using System.Linq;

namespace Godot;

/// <summary>
/// Base class of overlapping system with shape management.
/// Shape management needs to override to implement.
/// </summary>
public abstract partial class OverlapManager2D : Overlap2D
{
    /// <summary>
    /// Shape2D and its (Global) Transform used to query.
    /// </summary>
    public struct ShapeInfo
    {
        public Shape2D Shape { get ;set; }
        public Transform2D Transform { get ;set; }

        public ShapeInfo(Shape2D shape, Transform2D transform) => (Shape, Transform) = (shape, transform);
    }

    /// <summary>
    /// The manager will call this method to do overlapping query.
    /// Override to implement.
    /// </summary>
    protected abstract IEnumerable<ShapeInfo> GetShapeInfos();

    private void SetShapeInfo(ShapeInfo info, Vector2 deltaPos = default)
    {
        QueryParameters.Shape = info.Shape;
        QueryParameters.Transform = info.Transform with
        {
            Origin = info.Transform.Origin + deltaPos
        };
    }
    
    public IEnumerable<OverlapResult2D<T>> GetOverlappingObjects<T>(Func<OverlapResult2D<T>, bool> filter, Vector2 deltaPos = default, bool excludeOthers = false) where T : GodotObject
    {
        var infos = GetShapeInfos().ToArray();
        if (infos.Length == 1)
        {
            SetShapeInfo(infos[0], deltaPos);
            foreach (var result in QueryOverlappingObjects(filter, excludeOthers))
            {
                yield return result;
            }
            
            yield break;
        }

        var hash = new HashSet<OverlapResult2D<T>>();
        foreach (var info in infos)
        {
            SetShapeInfo(info, deltaPos);
            foreach (var result in QueryOverlappingObjects(filter, excludeOthers))
            {
                if (hash.Add(result))
                {
                    yield return result;
                }
            }
        }
    }
    
    public IEnumerable<OverlapResult2D<T>> GetOverlappingObjects<T>(Vector2 deltaPos = default, bool excludeOthers = false) where T : GodotObject
        => GetOverlappingObjects<T>(null, deltaPos, excludeOthers);
    public IEnumerable<OverlapResult2D<GodotObject>> GetOverlappingObjects(Func<OverlapResult2D<GodotObject>, bool> filter, Vector2 deltaPos = default, bool excludeOthers = false)
        => GetOverlappingObjects<GodotObject>(filter, deltaPos, excludeOthers);
    public IEnumerable<OverlapResult2D<GodotObject>> GetOverlappingObjects(Vector2 deltaPos = default)
        => GetOverlappingObjects<GodotObject>(null, deltaPos);
    
    public bool IsOverlapping<T>(Func<OverlapResult2D<T>, bool> filter, Vector2 deltaPos = default, bool excludeOthers = false) where T : GodotObject
    {
        foreach (var info in GetShapeInfos())
        {
            SetShapeInfo(info, deltaPos);
            if (QueryOverlappingObjects(filter, excludeOthers).Any())
            {
                return true;
            }
        }

        return false;
    }
    
    public bool IsOverlapping<T>(Vector2 deltaPos = default, bool excludeOthers = false) where T : GodotObject
        => IsOverlapping<T>(null, deltaPos, excludeOthers);
    public bool IsOverlapping(Func<OverlapResult2D<GodotObject>, bool> filter, Vector2 deltaPos = default, bool excludeOthers = false)
        => IsOverlapping<GodotObject>(filter, deltaPos, excludeOthers);
    public bool IsOverlapping(Vector2 deltaPos = default)
        => IsOverlapping<GodotObject>(null, deltaPos);
    
    public bool IsOverlappingWith(GodotObject col, Vector2 deltaPos = default, bool excludeOthers = false)
        => IsOverlapping(result => result.Collider == col, deltaPos, excludeOthers);
}