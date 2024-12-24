using System;
using System.Collections.Generic;

namespace Component;

/// <summary>
/// behavior disable management
/// </summary>
public class DisableList
{
    private List<string> List = [];
    
    public Action OnDisabled { private get; init; }
    public Action OnEnabled { private get; init; }

    public void Disable(string reason)
    {
        if (List.Count == 0)
        {
            OnDisabled?.Invoke();
        }
        List.Add(reason);
    }
    
    public void Enable(string reason)
    {
        List.Remove(reason);
        if (List.Count == 0)
        {
            OnEnabled?.Invoke();
        }
    }
    
    public bool IsDisabled() => List.Count > 0;
}