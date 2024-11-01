using System;
using System.Collections.Generic;
using GodotTask;
using Component;
using Utils;

namespace Godot;

// provide a simplified workflow for creating user interfaces in Godot
// it only handles the logic of menu

[GlobalClass]
public partial class MenuControl : Node
{
    [ExportCategory("MenuControl")]
    [Export]
    public MenuItem DefaultItem { get; set; } = null;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    [Export]
    public bool KeyDisabled { get; set; } = false;
    
    [Export]
    public bool LoopSelection { get; set; } = false;
    
    [ExportGroup("ContinuousMoving", "Continuous")]
    [Export]
    public double ContinuousMoveDelay { get ; set; } = 0.5d;
    
    /// <summary>
    /// should not be changed during runtime
    /// </summary>
    [Export]
    public double ContinuousMoveInterval { get ; private set; } = 0.1d;
    
    [ExportGroup("GuiBinding")]
    [Export]
    public MenuPanel Panel { get ;set; }
    
    [Export]
    public MenuScrollContainer ScrollContainer { get ;set; }
    
    [Export]
    public MenuItemList ItemList { get ;set; }
    
    [Signal]
    public delegate void MovedEventHandler();
    
    [Signal]
    public delegate void SelectedEventHandler(MenuItem item);
    
    public MenuItem CurrentItem { get ;set; }
    public List<MenuItem> Items { get ;set; } = new();

    public MenuControl() : base()
    {
        ChildEnteredTree += child =>
        {
            if (child is MenuItem item)
            {
                DefaultItem ??= item;
                item.Menu = this;
                Items.Add(item);
            }
        };
    
        TreeEntered += () =>
        {
            if (Panel != null) Panel.Menu = this;
        
            this.AddPhysicsProcess((delta) =>
            {
                var disabled = Disabled || KeyDisabled;
                MoveProcess(disabled, delta);
                
                if (!disabled && IsSelected()) Select();
            });
        };
        
        Ready += () => CurrentItem = DefaultItem;
    }

    public void AddItem(MenuItem item)
    {
        if (item != null && item.Menu != this)
        {
            item.Menu = this;
            Items.Add(item);
        }
    }
    
    public void AddItem(MenuItemRect itemRect) => AddItem(itemRect.MenuItem);
    
    public virtual bool IsMovingPrev() => Input.IsActionPressed("Up");
    public virtual bool IsMovingNext() => Input.IsActionPressed("Down");
    public virtual bool IsSelected() => Input.IsActionJustPressed("Select");
    
    public void Select()
    {
        if (!IsInstanceValid(CurrentItem)) return;
        
        CurrentItem.EmitSignal(MenuItem.SignalName.Selected);
        EmitSignal(SignalName.Selected, CurrentItem);
    }
    
    private Tracker<int> ContinuousDirTracker { get ; set; }
    private STimer ContinuousMoveTimer { get ; set; }

    public virtual void MoveProcess(bool disabled, double delta)
    {
        ContinuousDirTracker ??= new(0);
        if (ContinuousMoveTimer == null && ContinuousMoveDelay > 0d && ContinuousMoveInterval > 0d)
        {
            ContinuousMoveTimer = new(ContinuousMoveInterval);
        }

        if (disabled)
        {
            ContinuousMoveTimer?.Clear();
            ContinuousDirTracker.Reset(0);
            return;
        }
        
        var dir = Convert.ToInt16(IsMovingNext())
                  - Convert.ToInt16(IsMovingPrev());
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

    private void TryMove(int dir)
    {
        if (dir > 0) TryMoveNext();
        else if (dir < 0) TryMovePrev();
    }

    public virtual void TryMoveNext(bool emit = true)
    {
        var index = Items.IndexOf(CurrentItem);
        for (int i = index + 1; i < Items.Count; i++)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                if (emit) EmitSignal(SignalName.Moved);
                return;
            }
        }
        
        if (!LoopSelection) return;
        
        for (int i = 0; i < index; i++)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                if (emit) EmitSignal(SignalName.Moved);
                return;
            }
        }
    }

    public virtual void TryMovePrev(bool emit = true)
    {
        var index = Items.IndexOf(CurrentItem);
        for (int i = index - 1; i >= 0; i--)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                if (emit) EmitSignal(SignalName.Moved);
                return;
            }
        }

        if (!LoopSelection) return;

        for (int i = Items.Count - 1; i > index; i--)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                if (emit) EmitSignal(SignalName.Moved);
                return;
            }
        }
    }

    public async GDTask GuiAppear(MenuItem item = null)
    {
        // update item

        if (item != null && Items.Contains(item) && !item.Disabled)
        {
            CurrentItem = item;
            if (IsInstanceValid(ScrollContainer))
                ScrollContainer.ForceUpdate();
        }
        else if (CurrentItem == null)
        {
            CurrentItem = Items.Find(i => !i.Disabled);
            if (CurrentItem != null && IsInstanceValid(ScrollContainer))
                ScrollContainer.ForceUpdate();
        }
        
        if (IsInstanceValid(ItemList)) ItemList.Sort();
        
        if (IsInstanceValid(Panel)) await Panel.Appear();
        else
        {
            await Async.Wait(this, 0.1d);
            Disabled = false;
        }
    }

    public async GDTask GuiDisappear()
    {
        if (IsInstanceValid(Panel)) await Panel.Disappear();
        else
        {
            Disabled = true;
            await Async.Wait(this, 0.1d);
        }
    }
}