using BaseLib.Extensions;
using Falconer.FalconerCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Falconer.FalconerCode.Falcon;

/// <summary>
/// The falcon on screen: a sprite on the Falconer's shoulder. It flies off when Sent and swoops back on return.
/// Purely visual (reads FalconCmd state, never changes it), so it can't desync multiplayer.
/// </summary>
public static class FalconOverlay
{
    private const string NodeName = "FalconerFalcon";
    private const float Size = 150f;
    private static Texture2D? _texture;

    private static Texture2D? Texture =>
        _texture ??= ResourceLoader.Load<Texture2D>("falcon_falconer.png".CharacterUiPath());

    /// <summary>Shows the falcon perched, or hidden while Away.</summary>
    public static void Refresh(Creature owner)
    {
        var s = Ensure(owner);
        if (s == null) return;
        s.Visible = !FalconCmd.IsAway(owner);
        if (s.Visible) s.Position = PerchPos(s);
    }

    public static void Leave(Creature owner)
    {
        var s = Ensure(owner);
        if (s == null || !s.Visible) return;
        var start = PerchPos(s);
        s.Position = start;
        var t = s.CreateTween();
        t.TweenProperty(s, "position", start + new Vector2(260, -420), 0.45f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.Parallel().TweenProperty(s, "modulate:a", 0f, 0.45f);
        t.TweenCallback(Callable.From(() => { if (GodotObject.IsInstanceValid(s)) s.Visible = false; }));
    }

    public static void Land(Creature owner)
    {
        var s = Ensure(owner);
        if (s == null) return;
        var end = PerchPos(s);
        s.Visible = true;
        s.Position = end + new Vector2(-320, -380);
        s.Modulate = new Color(1, 1, 1, 0);
        var t = s.CreateTween();
        t.TweenProperty(s, "position", end, 0.4f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(s, "modulate:a", 1f, 0.2f);
    }

    public static void Hide(Creature owner)
    {
        var node = owner.GetCreatureNode();
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        node.GetNodeOrNull<Node>(NodeName)?.QueueFree();
    }

    private static Sprite2D? Ensure(Creature owner)
    {
        var node = owner.GetCreatureNode();
        if (node == null || !GodotObject.IsInstanceValid(node) || Texture == null) return null;
        var s = node.GetNodeOrNull<Sprite2D>(NodeName);
        if (s != null) return s;
        s = new Sprite2D { Name = NodeName, Texture = Texture, ZIndex = 40 };
        var scale = Size / Math.Max(Texture.GetWidth(), 1);
        s.Scale = new Vector2(scale, scale);
        node.AddChild(s);
        s.Position = PerchPos(s);
        return s;
    }

    /// <summary>On the raised glove, at the right shoulder, measured from the creature's Bounds if present.</summary>
    private static Vector2 PerchPos(Node2D s)
    {
        if (s.GetParent() is Control parent && FindBounds(parent) is { } b)
        {
            var r = b.GetRect();
            return new Vector2(r.Position.X + r.Size.X * 0.78f, r.Position.Y + r.Size.Y * 0.22f);
        }
        return new Vector2(70, -330);
    }

    private static Control? FindBounds(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control c && c.Name == "Bounds") return c;
            if (FindBounds(child) is { } found) return found;
        }
        return null;
    }
}
