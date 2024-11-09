using System;
using System.Collections.Generic;
using Utils;

namespace Godot;

[GlobalClass, Tool]
public partial class MenuScrollContainer : Control
{
    public enum MenuScrollContainerProcessCallback { Idle ,Physics }
    
    [ExportCategory("MenuScrollContainer")]
    [Export]
    public MenuScrollContainerProcessCallback ProcessCallback { get ;set; } = MenuScrollContainerProcessCallback.Idle;
    
    [ExportGroup("Horizontal")]
    [Export]
    public float ScrollHorizontal
    {
        get
        {
            var control = GetChildOrNull<Control>(0);
            if (control == null) return 0f;
            return -control.Position.X;
        }
        set
        {
            var control = GetChildOrNull<Control>(0);
            if (control != null)
                control.Position = control.Position with { X = -Math.Clamp(value, 0f, GetHorizontalMax()) };
        }
    }
    
    [Export(PropertyHint.Range, "0,1")]
    public float ScrollLeftMargin { get ;set; } = 0.2f;
    
    [Export(PropertyHint.Range, "0,1")]
    public float ScrollRightMargin { get ;set; } = 0.2f;
    
    [Export]
    public double ScrollHorizontalRate { get ;set; } = 20d;
    
    [ExportGroup("Vertical")]
    [Export]
    public float ScrollVertical
    {
        get
        {
            var control = GetChildOrNull<Control>(0);
            if (control == null) return 0f;
            return -control.Position.Y;
        }
        set
        {
            var control = GetChildOrNull<Control>(0);
            if (control != null)
                control.Position = control.Position with { Y = -Math.Clamp(value, 0f, GetVerticalMax()) };
        }
    }
    
    [Export(PropertyHint.Range, "0,1")]
    public float ScrollTopMargin { get ;set; } = 0.2f;
    
    [Export(PropertyHint.Range, "0,1")]
    public float ScrollBottomMargin { get ;set; } = 0.2f;
    
    [Export]
    public double ScrollVerticalRate { get ;set; } = 20d;

    protected List<MenuItemRect> ItemRects { get ;set; } = new();
    
    public MenuScrollContainer() : base()
    {
#if TOOLS
        if (Engine.IsEditorHint()) return;
#endif   

        TreeEntered += () =>
        {
            this.AddProcess((delta) =>
            {
                if (ScrollHorizontalRate > 0d)
                {
                    var x = GetCurrentFocusX();
                    if (x >= 0f)
                        ScrollToX(x, delta * ScrollHorizontalRate);
                }
                
                if (ScrollVerticalRate > 0d)
                {
                    var y = GetCurrentFocusY();
                    if (y >= 0f)
                        ScrollToY(y, delta * ScrollVerticalRate);
                }
            }, () => ProcessCallback == MenuScrollContainerProcessCallback.Physics);
        };
    
        Ready += () =>
        {
            AddItemRects(this);
            ForceUpdate();
        };
    }

    public void AddItemRects(Node node)
        => node.SetChildrenRecursively((child) =>
        {
            if (child is MenuItemRect rect) 
                ItemRects.Add(rect);
        });

    public void ForceUpdate()
    {
        ScrollToX(GetCurrentFocusX());
        ScrollToY(GetCurrentFocusY());
    }
    
    public float GetCurrentFocusX()
    {
        foreach (var rect in ItemRects)
        {
            if (rect.IsFocus())
            {
                return rect.Position.X + rect.Size.X / 2f;
            }
        }
        
        return -1f;
    }

    public float GetCurrentFocusY()
    {
        foreach (var rect in ItemRects)
        {
            if (rect.IsFocus())
            {
                return rect.Position.Y + rect.Size.Y / 2f;
            }
        }
        
        return -1f;
    }
    
    public float GetHorizontalMax()
    {
        if (GetChild(0) is Control control) 
            return Math.Max(0f, control.CustomMinimumSize.X - Size.X);
        return 0f;
    }

    public float GetVerticalMax()
    {
        if (GetChild(0) is Control control) 
            return Math.Max(0f, control.CustomMinimumSize.Y - Size.Y);
        return 0f;
    }
    
    public void ScrollToX(float x, double delta = -1d)
    {
        var w = Size.X;
        var left = ScrollHorizontal + ScrollLeftMargin * w;
        var right = ScrollHorizontal + (1f - ScrollRightMargin) * w;
        
        if (x >= left && x <= right) return;
        
        var target = x > right ? x - (1f - ScrollRightMargin) * w : x - ScrollLeftMargin * w;
        target = Math.Clamp(target, 0f, GetHorizontalMax());

        if (delta <= 0d)
        {
            ScrollHorizontal = target;
            return;
        }
        
        ScrollHorizontal = (float)Mathf.MoveToward(ScrollHorizontal, target,
            Math.Abs(ScrollHorizontal - target) * delta);
    }

    public void ScrollToY(float y, double delta = -1d)
    {
        var h = Size.Y;
        var top = ScrollVertical + ScrollTopMargin * h;
        var bottom = ScrollVertical + (1f - ScrollBottomMargin) * h;
        
        if (y >= top && y <= bottom) return;
        
        var target = y > bottom ? y - (1f - ScrollBottomMargin) * h : y - ScrollTopMargin * h;
        target = Math.Clamp(target, 0f, GetVerticalMax());

        if (delta <= 0d)
        {
            ScrollVertical = target;
            return;
        }
        
        ScrollVertical = (float)Mathf.MoveToward(ScrollVertical, target,
            Math.Abs(ScrollVertical - target) * delta);
    }
}