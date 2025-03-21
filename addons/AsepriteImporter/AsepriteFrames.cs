#if TOOLS

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using System.Linq;

namespace Editor.Addon;

/// <summary>
/// Create SpriteFrames resource through aseprite json files.
/// </summary>
public partial class AsepriteFrames
{
    public readonly struct FramesInfo(string t, Dictionary j, string a, bool l, bool tag)
    {
        public string TexPath { get; } = t;
        public Dictionary Json { get; } = j;
        public string AnimName { get; } = a;
        public bool Loop { get; } = l;
        public bool TagOnly { get; } = tag;
    }

    private FramesInfo[] Infos;

    public AsepriteFrames(IEnumerable<FramesInfo> infos) 
        => Infos = infos.ToArray();

    public SpriteFrames Create()
    {
        SpriteFrames spr = new();
        spr.RemoveAnimation("default");

        // use tag only if no relevant file exists
        if (Infos.Length == 1)
        {
            AddAnimation(spr, Infos[0], true);
            return spr;
        }

        foreach (var info in Infos)
        {
            AddAnimation(spr, info, info.TagOnly);
        }
        
        return spr;
    }

    private static void AddAnimation(SpriteFrames spr, FramesInfo info, bool tagOnly = false)
    {
        var json = info.Json;

        var frames = (Array<Dictionary>)json["frames"];
        var tags = (Array<Dictionary>)((Dictionary)json["meta"])["frameTags"];
        
        if (tags.Count > 0)
        {
            foreach (var tag in tags)
            {
                var repeat = 0;
                if (tag.TryGetValue("repeat", out var r)) repeat = (int)r;
            
                var name = (string)tag["name"];
                if (!tagOnly)
                    name = info.AnimName + "." + name;

                var from = (int)tag["from"];
                var to = (int)tag["to"];

                var dir = (string)tag["direction"];

                var tagFrames = frames.ToArray()[from..(to + 1)];

                AddFramesToAnimation(spr, info, repeat, name, tagFrames, dir);
            }

            return;
        }

        AddFramesToAnimation(spr, info, 0, info.AnimName, frames.ToArray());

    }

    private static void AddFramesToAnimation(SpriteFrames spr, FramesInfo info,
        int repeat, string animName, Dictionary[] frames, string direction = "forward")
    {
        // oneshot support
        var oneshot = animName.EndsWith("_oneshot");
        if (oneshot)
            animName = animName[..^8];

        if (spr.HasAnimation(animName)) { return; }

        spr.AddAnimation(animName);

        var minDuration = GetMinDuration(frames);
        var fps = GetFps(minDuration);

        var loop = !oneshot && repeat == 0 && info.Loop;
        spr.SetAnimationLoop(animName, loop);
        spr.SetAnimationSpeed(animName, fps);

        var reversed = direction is "reverse" or "pingpong_reverse";
        var pingpong = direction.StartsWith("pingpong") && frames.Length > 2;
        var iFrames = reversed ? frames.Reverse() : frames;
        
        if (repeat == 0)
        {
            if (pingpong)
            {
                if (!reversed)
                    iFrames = iFrames.Concat(frames[1..^1].Reverse());
                else
                    iFrames = iFrames.Concat(frames[1..^1]);
            }
        }
        else if (repeat > 1)
        {
            var arr = iFrames.ToArray();
            IEnumerable<Dictionary> result = [];
            if (pingpong)
            {
                var forward = arr[1..];
                var reverse = arr[..^1].Reverse().ToArray();
                result = result.Concat(arr);
                for (int i = 1; i < repeat; i++)
                {
                    result = result.Concat(i % 2 == 1 ? reverse : forward);
                }
            }
            else
            {
                for (int i = 0; i < repeat; i++)
                {
                    result = result.Concat(arr);
                }
            }
            iFrames = result;
        }

        var texture = GD.Load<Texture2D>(info.TexPath);
        texture.TakeOverPath(info.TexPath);

        System.Collections.Generic.Dictionary<Rect2, AtlasTexture> cachedTexture = new();

        foreach (var frame in iFrames)
        {
            var frameRect = (Dictionary)frame["frame"];
            Rect2 rect = new((float)frameRect["x"], (float)frameRect["y"],
                (float)frameRect["w"], (float)frameRect["h"]);

            AtlasTexture atlasTex;

            if (cachedTexture.TryGetValue(rect, out var value)) 
            { 
                atlasTex = value;
            }
            else
            {
                atlasTex = new()
                {
                    Atlas = texture,
                    Region = rect,
                    FilterClip = true
                };
            }

            var duration = (float)frame["duration"];

            spr.AddFrame(animName, atlasTex, duration/minDuration);
        }

    }

    private static float GetMinDuration(Dictionary[] frames)
    {
        var result = Mathf.Inf;
        foreach (var frame in frames)
        {
            var duration = (float)frame["duration"];
            result = duration < result ? duration : result;
        }

        return result;
    }

    private static float GetFps(float minDuration) => (float)Math.Ceiling(1000.0f / minDuration);

}

#endif