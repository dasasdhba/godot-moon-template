namespace Component;

public abstract class StateProcess
{
    public virtual void OnStateStart(StateProcess last) { }
    public virtual void OnStateEnd(StateProcess next) { }
    
    /// <summary>
    /// Return this to keep state, null to stop state machine, others to change state.
    /// </summary>
    public abstract StateProcess OnStateProcess(double delta);
}