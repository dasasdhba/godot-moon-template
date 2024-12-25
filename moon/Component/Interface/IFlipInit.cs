namespace Component;

/// <summary>
/// Node which can be flipped by NodeExtensions.TryInitFlip() call
/// </summary>
public interface IFlipInit
{
    public void FlipInit();
}