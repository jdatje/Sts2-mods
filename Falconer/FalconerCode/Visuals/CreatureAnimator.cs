using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Falconer.FalconerCode.Visuals;

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
        /// <summary>Distance from the body's origin down to its feet (local units), so breathing scales from the feet.</summary>
        public float Feet;
        public Vector2 ActionOffset;
        public float IdleY;
        public void Apply() => Body.Position = BasePos + ActionOffset + new Vector2(0, IdleY);
    }

    private static readonly Dictionary<ulong, State> States = [];

    /// <summary>The glow colour for Skills and Powers.</summary>
    public static Color Accent = new("e0a050");

    public static void Start(Creature creature)
    {
        var s = Get(creature);
        if (s == null || s.Dead) return;
        if (s.Idle != null && s.Idle.IsValid()) return;

        void Breathe(float k)
        {
            if (!GodotObject.IsInstanceValid(s.Body)) return;
            var f = new Vector2(1f - 0.01f * k, 1f + 0.02f * k);
            s.Body.Scale = s.BaseScale * f;
            s.IdleY = -(f.Y - 1f) * s.Feet * s.BaseScale.Y; // keep the feet on the ground
            s.Apply();
        }
        s.Idle = s.Body.CreateTween().SetLoops();
        s.Idle.TweenMethod(Callable.From<float>(Breathe), 0f, 1f, 1.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        s.Idle.TweenMethod(Callable.From<float>(Breathe), 1f, 0f, 1.6f)
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
        Move(s, s.Action!, Vector2.Zero, new Vector2(45, -6), 0.08f, Tween.EaseType.Out);
        Move(s, s.Action!, new Vector2(45, -6), Vector2.Zero, 0.22f, Tween.EaseType.InOut);
    }

    public static void Cast(Creature creature)
    {
        var s = Begin(creature);
        if (s == null) return;
        var glow = s.BaseModulate.Lerp(Accent, 0.45f);
        s.Action!.SetParallel();
        Move(s, s.Action, Vector2.Zero, new Vector2(0, -12), 0.1f, Tween.EaseType.Out);
        s.Action.TweenProperty(s.Body, "modulate", glow, 0.1f);
        s.Action.Chain();
        Move(s, s.Action, new Vector2(0, -12), Vector2.Zero, 0.2f, Tween.EaseType.In);
        s.Action.TweenProperty(s.Body, "modulate", s.BaseModulate, 0.3f);
    }

    public static void Hit(Creature creature)
    {
        var s = Begin(creature);
        if (s == null) return;
        s.Action!.SetParallel();
        s.Action.TweenProperty(s.Body, "modulate", new Color(1f, 0.45f, 0.45f, s.BaseModulate.A), 0.06f);
        Move(s, s.Action, Vector2.Zero, new Vector2(-16, 0), 0.06f, Tween.EaseType.InOut);
        s.Action.Chain();
        Move(s, s.Action, new Vector2(-16, 0), new Vector2(6, 0), 0.06f, Tween.EaseType.InOut);
        s.Action.Chain();
        Move(s, s.Action, new Vector2(6, 0), Vector2.Zero, 0.08f, Tween.EaseType.InOut);
        s.Action.TweenProperty(s.Body, "modulate", s.BaseModulate, 0.25f);
    }

    public static void Die(Creature creature)
    {
        var s = Get(creature);
        if (s == null || s.Dead) return;
        s.Dead = true;
        Kill(s.Idle); Kill(s.Sway); Kill(s.Action);
        var t = s.Body.CreateTween().SetParallel();
        t.TweenProperty(s.Body, "scale", s.BaseScale, 0.2f);
        Move(s, t, s.ActionOffset, new Vector2(0, 40), 0.9f, Tween.EaseType.In);
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
        s.ActionOffset = Vector2.Zero;
        s.Apply();
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
            if (body is Sprite2D sprite) s.Feet = sprite.GetRect().End.Y;
            States[id] = s;
            body.TreeExiting += () => States.Remove(id);
            return s;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Tween the action offset (attack lunge, hit shake...) on top of the idle breathing.</summary>
    private static void Move(State s, Tween t, Vector2 from, Vector2 to, float time, Tween.EaseType ease)
    {
        t.TweenMethod(Callable.From<Vector2>(v =>
            {
                if (!GodotObject.IsInstanceValid(s.Body)) return;
                s.ActionOffset = v;
                s.Apply();
            }), from, to, time)
            .SetTrans(Tween.TransitionType.Quad).SetEase(ease);
    }

    private static void Kill(Tween? t)
    {
        if (t != null && t.IsValid()) t.Kill();
    }
}
