namespace Component;

public abstract class StateProcess
{
    public virtual void OnStateStart() {}
    public virtual void OnStateEnd() {}
    
    /// <summary>
    /// Return this to keep state, null to stop state machine, others to change state.
    /// </summary>
    public abstract StateProcess OnStateProcess(double delta);
}