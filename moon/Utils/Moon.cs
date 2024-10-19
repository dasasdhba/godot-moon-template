using Godot;
using System;

namespace Utils;

// useful but uncategorized functions

public static class Moon
{
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

    public static void SetChildrenRecursively(this Node node, Action<Node> action, bool includeInternal = false)
    {
        foreach (var child in node.GetChildren(includeInternal))
        {
            action?.Invoke(child);
            SetChildrenRecursively(child, action, includeInternal);
        }
    }
}