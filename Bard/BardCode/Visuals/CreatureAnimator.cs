using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Bard.BardCode.Visuals;

/// <summary>
/// Simple code-driven motion for our static character images (no Spine rig): idle breathing and sway,
/// a lunge when attacking, a pop and glow for Skills/Powers, a red flinch when hit, sinking away on death.
/// Works on the body node of the creature's NCreatureVisuals, relative to its original transform, and is
/// purely visual (no game state), so it can't desync multiplayer.
/// </summary>
public static class CreatureAnimator
{
    private sealed class State
    {
        public Node2D Body = null!;
        public Vector2 BasePos, BaseScale;
        public Color BaseModulate;
        public Tween? Idle, Sway, Action;
        public bool Dead;
    }

    private static readonly Dictionary<ulong, State> States = [];

    /// <summary>The glow colour for Skills and Powers.</summary>
    public static Color Accent = new("9b6fe0");

    public static void Start(Creature creature)
    {
        var s = Get(creature);
        if (s == null || s.Dead) return;
        if (s.Idle != null && s.Idle.IsValid()) return;

        s.Idle = s.Body.CreateTween().SetLoops();
        s.Idle.TweenProperty(s.Body, "scale", s.BaseScale * new Vector2(0.99f, 1.02f), 1.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        s.Idle.TweenProperty(s.Body, "scale", s.BaseScale, 1.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        s.Sway = s.Body.CreateTween().SetLoops();
        s.Sway.TweenProperty(s.Body, "rotation", Mathf.DegToRad(0.8f), 2.4f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        s.Sway.TweenProperty(s.Body, "rotation", Mathf.DegToRad(-0.8f), 2.4f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public static void Attack(Creature creature)
    {
        var s = Begin(creature);
        if (s == null) return;
        s.Action!.TweenProperty(s.Body, "position", s.BasePos + new Vector2(45, -6), 0.08f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        s.Action.TweenProperty(s.Body, "position", s.BasePos, 0.22f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
    }

    public static void Cast(Creature creature)
    {
        var s = Begin(creature);
        if (s == null) return;
        var glow = s.BaseModulate.Lerp(Accent, 0.45f);
        s.Action!.SetParallel();
        s.Action.TweenProperty(s.Body, "position", s.BasePos + new Vector2(0, -12), 0.1f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        s.Action.TweenProperty(s.Body, "modulate", glow, 0.1f);
        s.Action.Chain().TweenProperty(s.Body, "position", s.BasePos, 0.2f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        s.Action.TweenProperty(s.Body, "modulate", s.BaseModulate, 0.3f);
    }

    public static void Hit(Creature creature)
    {
        var s = Begin(creature);
        if (s == null) return;
        s.Action!.SetParallel();
        s.Action.TweenProperty(s.Body, "modulate", new Color(1f, 0.45f, 0.45f, s.BaseModulate.A), 0.06f);
        s.Action.TweenProperty(s.Body, "position", s.BasePos + new Vector2(-16, 0), 0.06f);
        s.Action.Chain().TweenProperty(s.Body, "position", s.BasePos + new Vector2(6, 0), 0.06f);
        s.Action.Chain().TweenProperty(s.Body, "position", s.BasePos, 0.08f);
        s.Action.TweenProperty(s.Body, "modulate", s.BaseModulate, 0.25f);
    }

    public static void Die(Creature creature)
    {
        var s = Get(creature);
        if (s == null || s.Dead) return;
        s.Dead = true;
        Kill(s.Idle); Kill(s.Sway); Kill(s.Action);
        var t = s.Body.CreateTween().SetParallel();
        t.TweenProperty(s.Body, "position", s.BasePos + new Vector2(0, 40), 0.9f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.TweenProperty(s.Body, "modulate", new Color(s.BaseModulate, 0f), 0.9f);
        t.TweenProperty(s.Body, "rotation", 0f, 0.3f);
    }

    /// <summary>Stop all motion and restore the body (end of combat).</summary>
    public static void Stop(Creature creature)
    {
        var s = Get(creature, create: false);
        if (s == null) return;
        Kill(s.Idle); Kill(s.Sway); Kill(s.Action);
        if (GodotObject.IsInstanceValid(s.Body) && !s.Dead)
        {
            s.Body.Position = s.BasePos;
            s.Body.Scale = s.BaseScale;
            s.Body.Rotation = 0;
            s.Body.Modulate = s.BaseModulate;
        }
        States.Remove(s.Body.GetInstanceId());
    }

    // ------------------------------------------------------------------

    private static State? Begin(Creature creature)
    {
        var s = Get(creature);
        if (s == null || s.Dead) return null;
        Kill(s.Action);
        s.Body.Position = s.BasePos;
        s.Body.Modulate = s.BaseModulate;
        s.Action = s.Body.CreateTween();
        Start(creature);
        return s;
    }

    private static State? Get(Creature creature, bool create = true)
    {
        try
        {
            var node = creature.GetCreatureNode();
            if (node == null || !GodotObject.IsInstanceValid(node)) return null;
            var body = node.Visuals?.GetCurrentBody();
            if (body == null || !GodotObject.IsInstanceValid(body)) return null;
            var id = body.GetInstanceId();
            if (States.TryGetValue(id, out var s)) return s;
            if (!create) return null;
            s = new State { Body = body, BasePos = body.Position, BaseScale = body.Scale, BaseModulate = body.Modulate };
            States[id] = s;
            body.TreeExiting += () => States.Remove(id);
            return s;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Kill(Tween? t)
    {
        if (t != null && t.IsValid()) t.Kill();
    }
}
