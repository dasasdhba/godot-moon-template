using System;
using System.Collections.Generic;
using Component;

namespace Godot;

// menu option moves in four directions
// recommended to set menu item as a child of menu item rect
// then add rects manually here

public partial class MenuControlBox : MenuControl
{
    public List<MenuItemRect> ItemRects { get; set; } = new();

    public void ClearItemRects()
    {
        ItemRects.Clear();
        Items.Clear();
        CurrentItem = null;
    }

    public void AddItemRect(MenuItemRect itemRect)
    {
        ItemRects.Add(itemRect);
        AddItem(itemRect);
    }
    
    public virtual bool IsMovingLeft() => Input.IsActionPressed("Left");
    public virtual bool IsMovingRight() => Input.IsActionPressed("Right");
    public virtual bool IsMovingUp() => Input.IsActionPressed("Up");
    public virtual bool IsMovingDown() => Input.IsActionPressed("Down");

    private Tracker<Vector2I> ContinuousDirTracker { get ; set; }
    private STimer ContinuousMoveTimer { get ; set; }

    public override void MoveProcess(bool disabled, double delta)
    {
        ContinuousDirTracker ??= new(Vector2I.Zero);
        if (ContinuousMoveTimer == null && ContinuousMoveDelay > 0d && ContinuousMoveInterval > 0d)
        {
            ContinuousMoveTimer = new(ContinuousMoveInterval);
        }

        if (disabled)
        {
            ContinuousMoveTimer?.Clear();
            ContinuousDirTracker.Reset(Vector2I.Zero);
            return;
        }
        
        var x = Convert.ToInt16(IsMovingRight())
                  - Convert.ToInt16(IsMovingLeft());
        var y = Convert.ToInt16(IsMovingDown())
                  - Convert.ToInt16(IsMovingUp());
        var dir = new Vector2I(x, y);

        if (ContinuousDirTracker.Update(dir, delta))
        {
            ContinuousMoveTimer?.Clear();
            TryMove(dir);
        }
        else if (ContinuousMoveTimer != null && ContinuousDirTracker.Time >= ContinuousMoveDelay)
        {
            if (ContinuousMoveTimer.Update(delta))
            {
                TryMove(dir);
            }
        }
    }

    private void TryMove(Vector2I dir)
    {
        var current = CurrentItem.Rect;
        if (current == null) return;
        
        // using focus neighbours first

        bool TryMoveTo(MenuItemRect rect)
        {
            if (ItemRects.Contains(rect))
            {
                if (CurrentItem != rect.MenuItem)
                {
                    CurrentItem = rect.MenuItem;
                    EmitSignal(SignalName.Moved);
                }
                return true;
            }
            return false;
        }

        if (dir.X > 0)
        {
            if (TryMoveTo(current.GetNodeOrNull<MenuItemRect>(current.FocusNeighborRight)))
                return;
        }
        
        if (dir.X < 0)
        {
            if (TryMoveTo(current.GetNodeOrNull<MenuItemRect>(current.FocusNeighborLeft)))
                return;
        }
        
        if (dir.Y > 0)
        {
            if (TryMoveTo(current.GetNodeOrNull<MenuItemRect>(current.FocusNeighborBottom)))
                return;
        }

        if (dir.Y < 0)
        {
            if (TryMoveTo(current.GetNodeOrNull<MenuItemRect>(current.FocusNeighborTop)))
                return;
        }
        
        // using projection nearest one
        
        var c = current.GlobalPosition + current.Size / 2f;
        var d = -1f;
        var r = current;
        foreach (var rect in ItemRects)
        {
            if (rect == current) continue;
            
            var p = rect.GlobalPosition + rect.Size / 2f;
            var proj = (p - c).Dot(dir);
            if (proj > 0 && (d < 0f || proj < d))
            {
                d = proj;
                r = rect;
            }
        }
        
        if (r != current) TryMoveTo(r);
    }
}