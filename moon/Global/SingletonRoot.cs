using Godot;

namespace Global;

public partial class SingletonRoot : Node
{
    public SingletonRoot() : base()
    {
        Singleton.Root = this;
    }
}