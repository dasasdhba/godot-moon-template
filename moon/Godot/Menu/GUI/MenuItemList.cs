using System;
using System.Collections.Generic;
using Utils;

namespace Godot;

/// <summary>
/// Auto sort menu item rect
/// </summary>
[GlobalClass]
public partial class MenuItemList : Control
{
    [ExportCategory("MenuItemList")]
    [Export]
    public Vector2 SortOrigin { get ;set; } = new(0f, 0f);
    
    [Export]
    public Vector2 SortOffset { get ;set; } = new(0f, 32f);
    
    [Export]
    public float SortRate { get ;set; } = 20f;

    protected List<MenuItemRect> ItemRects { get; set; } = new();

    protected class RectComparer : IComparer<MenuItemRect>
    {
        public int Compare(MenuItemRect x, MenuItemRect y)
        {
            return GetIndex(x) - GetIndex(y);
        }

        private static int GetIndex(MenuItemRect rect)
        {
            var item = rect.MenuItem;
            var menu = item.Menu;
            return menu.Items.IndexOf(item);
        }
    }

    public MenuItemList() : base()
    {
        ChildEnteredTree += (child) =>
        {
            if (child is MenuItemRect rect) ItemRects.Add(rect);
        };
        
        Ready += () =>
        {
            ItemRects.Sort(new RectComparer());
            Sort();
            this.AddProcess(delta => Sort((float)(delta * SortRate)));
        };
    }
        
    public void Sort(float delta = -1f)
    {
        var rects = ItemRects.FindAll(r => r.Visible);
        var max = Vector2.Zero;
        
        for (int i = 0; i < rects.Count; i++)
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
                rect.Position = rect.Position.MoveToward(target, delta);
            }
        }
        
        CustomMinimumSize = max;
    }
}