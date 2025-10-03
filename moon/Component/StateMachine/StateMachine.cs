using Godot;
using Utils;

namespace Component;

/// <summary>
/// Light weighted state machine which can bind with godot's node.
/// useful to create easy logic.
/// </summary>
public partial class StateMachine
{
    private StateProcess _CurrentState;

    public StateProcess CurrentState
    {
        get => _CurrentState;
        set
        {
            if (_CurrentState == value) return;
            
            _CurrentState?.OnStateEnd(value);
            value?.OnStateStart(_CurrentState);
            _CurrentState = value;
        }
    }
    
    public void Update(double delta) 
         => CurrentState = CurrentState?.OnStateProcess(delta);
}

public partial class StateMachineNode(StateProcess firstState, bool physics = false, bool oneshot = false) : Node
{
    private StateMachine _StateMachine = new();
    private StateProcess _FirstState = firstState;
    private bool _Physics = physics;
    private bool _Oneshot = oneshot;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        
        Launch(_FirstState);
        this.AddProcess(Process, _Physics);
    }

    private void Process(double delta)
    {
        _StateMachine.Update(delta);
        
        if (_Oneshot && !IsAlive()) QueueFree();
    }
    
    public StateProcess CurrentState => _StateMachine.CurrentState;
    public void Launch(StateProcess state) => _StateMachine.CurrentState = state;
    public void Stop() => _StateMachine.CurrentState = null;
    public bool IsAlive() => _StateMachine.CurrentState != null;
}

public static partial class StateMachineExtensions
{
    public static StateMachineNode LaunchState(this Node node, StateProcess state, bool physics = false, bool oneshot = false)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{node} namely {node.GetPathTo(node.GetTree().GetEditedSceneRoot())} is trying to call LaunchState in editor, which is not expected.");
            return null;
        }
    #endif
    
        var binding = new StateMachineNode(state, physics, oneshot);
        binding.BindParent(node);
        node.AddChild(binding, false, Node.InternalMode.Front);
        return binding;
    }
    
    public static StateMachineNode LaunchPhysicsState(this Node node, StateProcess state, bool oneshot = false)
        => node.LaunchState(state, true, oneshot);
}