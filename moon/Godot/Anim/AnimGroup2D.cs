using System.Collections.Generic;
using Utils;

namespace Godot;

/// <summary>
/// AnimatedSprite2D Group.
/// </summary>
[GlobalClass]
public partial class AnimGroup2D : Node2D
{
    [ExportCategory("AnimGroup2D")]
    [ExportGroup("Animation")]
    [Export]
    public string CurrentSprite { get ;set; }
    
    [Export]
    public string Autoplay { get ;set; }
    
    [Export]
    public float SpeedScale { get ;set; } = 1f;
    
    [ExportGroup("Offset")]
    [Export]
    public bool Centered { get ;set; } = true;
    
    [Export]
    public Vector2 Offset { get ;set; } = Vector2.Zero;
    
    [Export]
    public bool FlipH { get ;set; } = false;
    
    [Export]
    public bool FlipV { get ;set; } = false;
    
    public List<AnimatedSprite2D> Sprites { get ;set; } = [];

    public AnimGroup2D() : base()
    {
        ChildEnteredTree += node =>
        {
            if (node is AnimatedSprite2D sprite)
            {
                Sprites.Add(sprite);
            }
        };
        
        ChildExitingTree += node =>
        {
            if (node is AnimatedSprite2D sprite)
            {
                Sprites.Remove(sprite);
            }
        };
        
        TreeEntered += () =>
        {
            if (Autoplay != "") Play(Autoplay);
            UpdateSprites();
            this.AddProcess(UpdateSprites);
        };
    }

    public void UpdateSprites()
    {
        foreach (var sprite in Sprites)
        {
            if (CurrentSprite != "")
            {
                sprite.Visible = sprite.Name == CurrentSprite;
            }
            
            sprite.FlipH = FlipH;
            sprite.FlipV = FlipV;
            sprite.Centered = Centered;
            sprite.Offset = Offset;
            sprite.SpeedScale = SpeedScale;
        }
    }

    public void Play(StringName animation = default, float customSpeed = 1f, bool fromEnd = false)
    {
        foreach (var sprite in Sprites)
        {
            sprite.Play(animation, customSpeed, fromEnd);
        }
    }

    public void PlayBackwards(StringName animation = default)
    {
        foreach (var sprite in Sprites)
        {
            sprite.PlayBackwards(animation);
        }
    }

    public void Pause()
    {
        foreach (var sprite in Sprites)
        {
            sprite.Pause();
        }
    }

    public void Stop()
    {
        foreach (var sprite in Sprites)
        {
            sprite.Stop();
        }
    }
    
    public string GetAnimation(int index = 0)
       => Sprites[index].Animation;
       
    public int GetFrame(int index = 0)
       => Sprites[index].Frame;
       
    public float GetFrameProgress(int index = 0)
       => Sprites[index].FrameProgress;
       
    public SpriteFrames GetSpriteFrames(int index = 0)
       => Sprites[index].SpriteFrames;
       
    public float GetPlayingSpeed(int index = 0)
       => Sprites[index].GetPlayingSpeed();
       
    public bool IsPlaying(int index = 0)
       => Sprites[index].IsPlaying();
}