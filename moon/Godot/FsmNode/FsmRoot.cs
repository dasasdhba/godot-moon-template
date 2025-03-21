using System.Collections.Generic;

namespace Godot;

[GlobalClass]
public partial class FsmRoot : Node
{
    [ExportCategory("FsmRoot")]
    [Export]
    public string State
    {
        get => _State;
        set
        {
            if (_State == value) return;
            _State = value;
            if (IsInsideTree())
                UpdateFsm();
        }
    }
    
    private string _State = "";
    
    private List<FsmNode> FsmNodes = [];

    public FsmRoot() : base()
    {
        ChildEnteredTree += child =>
        {
            if (child is FsmNode fsmNode)
            {
                FsmNodes.Add(fsmNode);
            }
        };
        
        ChildExitingTree += child =>
        {
            if (child is FsmNode fsmNode)
            {
                FsmNodes.Remove(fsmNode);
            }
        };
        
        Ready += UpdateFsm;
    }

    private void UpdateFsm()
    {
        foreach (var f in FsmNodes)
        {
            if (f.State != State) f.FsmStop();
            else f.FsmStart();
        }
    }
}