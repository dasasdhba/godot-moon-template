using System;
using System.Collections.Generic;

namespace Godot;

public partial class Draw2D : Node2D
{
    // drawing API used in DrawProcess call.

    protected enum Draw2DBlendMode
    {
        Mix,
        Add,
        Sub,
        Mul,
        PremultAlpha
    }

    private Draw2DBlendMode BlendMode = Draw2DBlendMode.Mix;

    /// <summary>
    /// Blend mode will be overridden by custom material.
    /// </summary>
    protected void SetBlendMode(Draw2DBlendMode blendMode) => BlendMode = blendMode;
    protected void ResetBlendMode() => BlendMode = Draw2DBlendMode.Mix;

    private Dictionary<Draw2DBlendMode, Material> BlendMaterialMap = new()
    {
        { Draw2DBlendMode.Mix, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Mix }
        },
        { Draw2DBlendMode.Add, new CanvasItemMaterial() { 
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add } 
        },
        { Draw2DBlendMode.Sub, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Sub } 
        },
        { Draw2DBlendMode.Mul, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Mul } 
        },
        { Draw2DBlendMode.PremultAlpha, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha } 
        }
    };

    private Material DrawMaterial = null;

    /// <summary>
    /// Non-null material will override blend mode setting.
    /// </summary>
    protected void SetDrawMaterial(Material material) => DrawMaterial = material;
    protected void ResetDrawMaterial() => DrawMaterial = null;

    private Color DrawModulate = new(1f, 1f, 1f);

    protected void SetDrawModulate(Color modulate) => DrawModulate = modulate;
    protected void SetDrawModulateRGB(Color modulate)
    {
        DrawModulate.R = modulate.R;
        DrawModulate.G = modulate.G;
        DrawModulate.B = modulate.B;
    }
    protected void SetDrawModulateAlpha(float alpha) => DrawModulate.A = alpha;
    protected void ResetDrawModulate() => DrawModulate = new(1f, 1f, 1f);

    private Transform2D DrawTransform = new(0f, new Vector2(0f, 0f));
    private bool DrawGlobal = false;
    
    protected void SetDrawGlobal(bool global) => DrawGlobal = global;
    protected void SetDrawTransform(Transform2D transform) => DrawTransform = transform;
    protected void ResetDrawTransform()
    {
        DrawTransform = new(0f, new Vector2(0f, 0f));
        DrawGlobal = false;
    }
    protected void SetDrawPosition(Vector2 pos) => DrawTransform.Origin = pos;
    protected void SetDrawRotation(float rotation) => DrawTransform = new(rotation, 
        DrawTransform.Scale, DrawTransform.Skew, DrawTransform.Origin);
    protected void SetDrawScale(Vector2 scale) => DrawTransform = new(DrawTransform.Rotation,
        scale, DrawTransform.Skew, DrawTransform.Origin);
    protected void SetDrawSkew(float skew) => DrawTransform = new(DrawTransform.Rotation,
        DrawTransform.Scale, skew, DrawTransform.Origin);

    private int DrawZIndex = 0;
    protected void SetDrawZIndex(int zIndex) => DrawZIndex = zIndex;
    protected void ResetDrawZIndex() => DrawZIndex = 0;

    protected void AddDrawingTask(Action<Drawer> task)
    {
        var QueuedMaterial = DrawMaterial ?? BlendMaterialMap[BlendMode];
        var QueuedModulate = DrawModulate;
        var QueuedTransform = DrawTransform;
        var QueuedGlobal = DrawGlobal;
        int QueuedZIndex = DrawZIndex;

        QueuedDrawingTasks.Add((Drawer drawer) =>
        {
            drawer.Material = QueuedMaterial;
            drawer.Modulate = QueuedModulate;
            if (QueuedGlobal) drawer.GlobalTransform = QueuedTransform;
            else drawer.Transform = QueuedTransform;
            drawer.ZIndex = QueuedZIndex;
            drawer.Position += Offset;
            if (FlipH)
            {
                drawer.Scale = drawer.Scale with { X = drawer.Scale.X * -1f };
                drawer.Position = drawer.Position with { X = drawer.Position.X * -1f };
            }
            if (FlipV)
            {
                drawer.Scale = drawer.Scale with { Y = drawer.Scale.Y * -1f };
                drawer.Position = drawer.Position with { Y = drawer.Position.Y * -1f };
            }
            drawer.ForceUpdateTransform();
            drawer.DrawingTask = () => task(drawer);
        });
    }

    // line

    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawLine(Vector2, Vector2, Color, float, bool)"/>
    /// </summary>
    protected void QueuedDrawLine(Vector2 from, Vector2 to, Color color, 
        float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawLine(from, to, color, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultiline(Vector2[], Color, float)"/>
    /// </summary>
    protected void QueuedDrawMultiline(Vector2[] points, Color color, float width = -1f)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawMultiline(points, color, width);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultilineColors(Vector2[], Color[], float)"/>
    /// </summary>
    protected void QueuedDrawMultilineColors(Vector2[] points, Color[] colors, float width = -1f)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawMultilineColors(points, colors, width);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolyline(Vector2[], Color, float, bool)"/>
    /// </summary>
    protected void QueuedDrawPolyline(Vector2[] points, Color color,
        float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawPolyline(points, color, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolylineColors(Vector2[], Color[], float, bool)"/>
    /// </summary>
    protected void QueuedDrawPolylineColors(Vector2[] points, Color[] colors,
        float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawPolylineColors(points, colors, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawDashedLine(Vector2, Vector2, Color, float, float, bool)"/>
    /// </summary>
    protected void QueuedDrawDashedLine(Vector2 from, Vector2 to, Color color,
        float width = -1f, float dash = 2f, bool aligned = true)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawDashedLine(from, to, color, width, dash, aligned);
        });
    }

    // shape
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawArc(Vector2, float, float, float, int, Color, float, bool)"/>
    /// </summary>
    protected void QueuedDrawArc(Vector2 center, float radius, float startAngle, float endAngle,
        Color color, int pointCount = 128, float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawArc(center, radius, startAngle, endAngle, pointCount,
                color, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawCircle(Vector2, float, Color)"/>
    /// </summary>
    protected void QueuedDrawCircle(Vector2 center, float radius, Color color,
        bool filled = true, float width = -1f, bool anitiliased = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawCircle(center, radius, color, filled, width, anitiliased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawRect(Rect2, Color, bool, float)"/>
    /// </summary>
    protected void QueuedDrawRect(Rect2 rect, Color color, bool filled = true,
        float width = -1f)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawRect(rect, color, filled, width);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolygon(Vector2[], Color[], Vector2[], Texture2D)"/>
    /// </summary>
    protected void QueuedDrawPolygon(Vector2[] points, Color[] colors,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawPolygon(points, colors, uvs, texture);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawColoredPolygon(Vector2[], Color, Vector2[], Texture2D)"/>
    /// </summary>
    protected void QueuedDrawColoredPolygon(Vector2[] points, Color color,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawColoredPolygon(points, color, uvs, texture);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPrimitive(Vector2[], Color[], Vector2[], Texture2D)"/>
    /// </summary>
    protected void QueuedDrawPrimitive(Vector2[] points, Color[] colors,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawPrimitive(points, colors, uvs, texture);
        });
    }

    // string

    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawString(Font, Vector2, string, HorizontalAlignment, float, int, Color?, TextServer.JustificationFlag, TextServer.Direction, TextServer.Orientation)"/>
    /// </summary>
    protected void QueuedDrawString(Font font, Vector2 pos, string text,
        float width = -1f, int fontSize = 16, Color? modulate = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        TextServer.JustificationFlag justificationFlags =
        TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
        TextServer.Direction direction = TextServer.Direction.Auto,
        TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawString(font, pos, text, alignment, width, fontSize, modulate,
                justificationFlags, direction, orientation);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawStringOutline(Font, Vector2, string, HorizontalAlignment, float, int, int, Color?, TextServer.JustificationFlag, TextServer.Direction, TextServer.Orientation)"/>
    /// </summary>
    protected void QueuedDrawStringOutline(Font font, Vector2 pos, string text,
        float width = -1f, int fontSize = 16, int size = 1, Color? modulate = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        TextServer.JustificationFlag justificationFlags =
        TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
        TextServer.Direction direction = TextServer.Direction.Auto,
        TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawStringOutline(font, pos, text, alignment, width, fontSize, size,
                modulate, justificationFlags, direction, orientation);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultilineString(Font, Vector2, string, HorizontalAlignment, float, int, int, Color?, TextServer.LineBreakFlag, TextServer.JustificationFlag, TextServer.Direction, TextServer.Orientation)"/>
    /// </summary>
    protected void QueuedDrawMultilineString(Font font, Vector2 pos, string text,
        float width = -1f, int fontSize = 16, int maxLines = -1, Color? modulate = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        TextServer.LineBreakFlag brkFlags = 
        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound,
        TextServer.JustificationFlag justificationFlags =
        TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
        TextServer.Direction direction = TextServer.Direction.Auto,
        TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawMultilineString(font, pos, text, alignment, width, fontSize, 
                maxLines, modulate, brkFlags, justificationFlags, 
                direction, orientation);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultilineStringOutline(Font, Vector2, string, HorizontalAlignment, float, int, int, int, Color?, TextServer.LineBreakFlag, TextServer.JustificationFlag, TextServer.Direction, TextServer.Orientation)"/>
    /// </summary>
    protected void QueuedDrawMultilineStringOutline(Font font, Vector2 pos, string text,
        float width = -1f, int fontSize = 16, int size = 1, int maxLines = -1, Color? modulate = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        TextServer.LineBreakFlag brkFlags =
        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound,
        TextServer.JustificationFlag justificationFlags =
        TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
        TextServer.Direction direction = TextServer.Direction.Auto,
        TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawMultilineStringOutline(font, pos, text, alignment, width, fontSize,
                maxLines, size, modulate, brkFlags, justificationFlags,
                direction, orientation);
        });
    }

    // texture
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTexture(Texture2D, Vector2, Color?)"/>
    /// </summary>
    protected void QueuedDrawTexture(Texture2D texture, Vector2 pos, Color? modulate = null)
    {
        if (Centered)
        {
            Vector2 texSize = new(texture.GetWidth(), texture.GetHeight());
            pos -= texSize / 2f;
        }
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTexture(texture, pos, modulate);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTextureRect(Texture2D, Rect2, bool, Color?, bool)"/>
    /// </summary>
    protected void QueuedDrawTextureRect(Texture2D texture, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTextureRect(texture, rect, tile, modulate, transpose);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTextureRectRegion(Texture2D, Rect2, Rect2, Color?, bool, bool)"/>
    /// </summary>
    protected void QueuedDrawTextureRectRegion(Texture2D texture, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTextureRectRegion(texture, rect, srcRect, modulate, transpose, clipUV);
        });
    }

    // sprite frames
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTexture"/>
    /// </summary>
    protected void QueuedDrawSpriteFrames(SpriteFrames spr, string animation, int frame, 
        Vector2 pos, Color? modulate = null)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        if (Centered)
        {
            Vector2 texSize = new(texture.GetWidth(), texture.GetHeight());
            pos -= texSize / 2f;
        } 
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTexture(texture, pos, modulate);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRect"/>
    /// </summary>
    protected void QueuedDrawSpriteFramesRect(SpriteFrames spr, string animation, int frame,
        Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTextureRect(texture, rect, tile, modulate, transpose);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRectRegion"/>
    /// </summary>
    protected void QueuedDrawSpriteFramesRectRegion(SpriteFrames spr, string animation, int frame,
        Rect2 rect, Rect2 srcRect, Color? modulate = null, 
        bool transpose = false, bool clipUV = true)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        AddDrawingTask((Drawer drawer) =>
        {
            drawer.DrawTextureRectRegion(texture, rect, srcRect, modulate, transpose, clipUV);
        });
    }

    // sprite2d
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTexture"/>
    /// </summary>
    protected void QueuedDrawSprite(Sprite2D spr, Vector2 pos, Color? modulate = null)
    {
        pos += spr.Offset;
        QueuedDrawTexture(spr.Texture, pos, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRect"/>
    /// </summary>
    protected void QueuedDrawSpriteRect(Sprite2D spr, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    {
        QueuedDrawTextureRect(spr.Texture, rect, tile, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRectRegion"/>
    /// </summary>
    protected void QueuedDrawSpriteRectRegion(Sprite2D spr, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        QueuedDrawTextureRectRegion(spr.Texture, rect, srcRect, modulate, transpose, clipUV);
    }

    // animated sprite
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFrames"/>
    /// </summary>
    protected void QueuedDrawAnimatedSprite(AnimatedSprite2D spr, Vector2 pos, Color? modulate = null)
    {
        pos += spr.Offset;
        QueuedDrawSpriteFrames(spr.SpriteFrames, spr.Animation, spr.Frame, pos, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFramesRect"/>
    /// </summary>
    protected void QueuedDrawAnimatedSpriteRect(AnimatedSprite2D spr, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    { 
        QueuedDrawSpriteFramesRect(spr.SpriteFrames, spr.Animation, spr.Frame, 
            rect, tile, modulate, transpose);
    }

    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFramesRectRegion"/>
    /// </summary>
    protected void QueuedDrawAnimatedSpriteRectRegion(AnimatedSprite2D spr, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        QueuedDrawSpriteFramesRectRegion(spr.SpriteFrames, spr.Animation, spr.Frame,
            rect, srcRect, modulate, transpose, clipUV);
    }
}