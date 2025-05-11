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
            
            _CurrentState?.OnStateEnd();
            _CurrentState = value;
            _CurrentState?.OnStateStart();
        }
    }
    
    private bool _Launched;
    public void Launch(StateProcess state)
    {
        CurrentState = state;
        _Launched = true;
    }
    public void Stop() => CurrentState = null;
    public bool IsAlive() => !_Launched || CurrentState != null;
    public void Update(double delta) 
         => CurrentState = CurrentState?.OnStateProcess(delta);
    
    public bool Paused { get ;set; } = false;
    
    public void Pause() => Paused = true;
    public void Resume() => Paused = false;
}

public static partial class StateMachineExtensions
{
    private partial class StateMachineNode(StateProcess firstState, bool physics = false) : Node
    {
        private StateMachine _StateMachine = new();
        public StateMachine GetStateMachine() => _StateMachine;
        
        private StateProcess _FirstState = firstState;
        private bool _Physics = physics;
        
        public override void _EnterTree()
        {
            base._EnterTree();
            this.AddProcess(Process, _Physics);
        }

        private void Process(double delta)
        {
            if (_StateMachine.Paused) return;

            if (_FirstState != null)
            {
                _StateMachine.Launch(_FirstState);
                _FirstState = null;
            }
            
            _StateMachine.Update(delta);
            
            if (!_StateMachine.IsAlive()) QueueFree();
        }
    }

    public static StateMachine LaunchState(this Node node, StateProcess state, bool physics = false)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{node} namely {node.GetPathTo(node.GetTree().GetEditedSceneRoot())} is trying to call LaunchState in editor, which is not expected.");
            return null;
        }
    #endif
    
        var binding = new StateMachineNode(state, physics);
        binding.BindParent(node);
        node.AddChild(binding, false, Node.InternalMode.Front);
        return binding.GetStateMachine();
    }
    
    public static StateMachine LaunchPhysicsState(this Node node, StateProcess state)
        => node.LaunchState(state, true);
}