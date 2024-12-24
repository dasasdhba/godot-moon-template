namespace Godot;

/// <summary>
/// Play audio in global scope(viewport) then free.
/// </summary>
[GlobalClass]
public partial class AudioOneshotPlayer : AudioStreamPlayer
{
    public AudioOneshotPlayer() :base()
    {
        Finished += QueueFree;
    }

    public void PlayOneshot()
    {
        CallDeferred(Node.MethodName.Reparent, GetViewport());
        Play();
    }
}