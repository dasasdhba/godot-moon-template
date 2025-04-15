using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Godot;

/// <summary>
/// Auto sort child menu rect.
/// </summary>
[GlobalClass]
public partial class MenuRectList : Control
{
    [ExportCategory("MenuRectList")]
    [Export]
    public Vector2 SortOrigin { get ;set; } = new(0f, 0f);
    
    [Export]
    public Vector2 SortOffset { get ;set; } = new(0f, 32f);
    
    [Export]
    public float SortRate { get ;set; } = 10f;
    
    public enum MenuRectListProcessCallback { Idle, Physics }
    [Export]
    public MenuRectListProcessCallback ProcessCallback { get ;set; } 
        = MenuRectListProcessCallback.Physics;

    public MenuRectList() : base()
    {
        TreeEntered += () => this.AddProcess(delta => 
            Sort((float)(delta * SortRate)),
            () => ProcessCallback == MenuRectListProcessCallback.Physics);
    }

    private IEnumerable<MenuRect> GetMenuRects()
        => this.GetChildrenCached<MenuRect>().Where(child => child.Visible);
        
    public void Sort(float delta = -1f)
    {
        var rects = GetMenuRects().ToArray();
        var max = Vector2.Zero;
        
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            
            var target = SortOrigin + SortOffset * i;
            max.X = Math.Max(max.X, target.X + rect.Size.X);
            max.Y = Math.Max(max.Y, target.Y + rect.Size.Y);
            
            if (delta <= 0f)
            {
                rect.Position = target;
            }
            else
            {
                rect.Position = rect.Position.MoveToward(target,
                   (target - rect.Position).Length() * delta);
            }
        }
        
        CustomMinimumSize = max;
    }
    
    public Vector2 GetRectSize() => CustomMinimumSize;
}