#if TOOLS

using System;
using Godot;
using Godot.Collections;
using System.Linq;

namespace Editor.Addon;

/// <summary>
/// Create SpriteFrames resource through aseprite json files.
/// </summary>
public partial class AsepriteFrames
{
    public struct FramesInfo
    {
        public string TexPath { get; set; }
        public Dictionary Json { get; set; }
        public string AnimName { get; set; }
        public bool Loop { get; set; }
        public bool TagOnly { get; set; }

        public FramesInfo(string t, Dictionary j, string a, bool l, bool tag) =>
            (TexPath, Json, AnimName, Loop, TagOnly) = (t, j, a, l, tag);

    }

    private FramesInfo[] Infos;

    public AsepriteFrames(System.Collections.Generic.IEnumerable<FramesInfo> infos) 
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
                var name = (string)tag["name"];
                if (!tagOnly)
                    name = info.AnimName + "." + name;

                var from = (int)tag["from"];
                var to = (int)tag["to"];

                var dir = (string)tag["direction"];

                var tagFrames = frames.ToArray()[from..(to + 1)];

                AddFramesToAnimation(spr, info, name, tagFrames, dir);
            }

            return;
        }

        AddFramesToAnimation(spr, info, info.AnimName, frames.ToArray());

    }

    private static void AddFramesToAnimation(SpriteFrames spr, FramesInfo info,
        string animName, Dictionary[] frames, string direction = "forward")
    {
        // oneshot support
        var oneshot = animName.EndsWith("_oneshot");
        if (oneshot)
            animName = animName[..^8];

        if (spr.HasAnimation(animName)) { return; }

        spr.AddAnimation(animName);

        var minDuration = GetMinDuration(frames);
        var fps = GetFps(minDuration);

        var loop = !oneshot && info.Loop;
        spr.SetAnimationLoop(animName, loop);
        spr.SetAnimationSpeed(animName, fps);

        var reversed = direction == "reverse" || direction == "pingpong_reverse";
        var iFrames = reversed ? frames.Reverse() : frames;

        if (direction.StartsWith("pingpong"))
        {
            if (!reversed)
                iFrames = iFrames.Concat(frames[1..^1].Reverse());
            else
                iFrames = iFrames.Concat(frames[1..^1]);
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