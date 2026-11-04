using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CardSharp.CardSharpCode.Table;

/// <summary>
/// Always-visible panel above the Card Sharp's head: one mini playing card per card on the Table,
/// faint empty slots for the rest, and a line like "3/5 · Pair". At a Showdown the cards glow gold and the
/// hand is shown for about a second, then the panel clears.
///
/// Built only from built-in Godot nodes (no custom script class), attached to the creature's NCreature node,
/// so it moves with her and is visible to every player in co-op.
/// </summary>
public static class TableOverlay
{
    private const string NodeName = "CardSharpTableOverlay";
    private const float CardW = 42, CardH = 58, Gap = 6, LineH = 30;
    private static readonly Color Cream = new("f6efdc");
    private static readonly Color Gold = new("d4a840");
    private static readonly Color Red = new("b8332b");
    private static readonly Color Ink = new("1c1a18");

    // Creatures whose overlay is showing a Showdown right now (refreshes wait until it is over).
    private static readonly HashSet<Creature> Frozen = [];

    /// <summary>Redraw from the current Table. Safe to call any time; does nothing outside combat.</summary>
    public static void Refresh(Creature owner)
    {
        if (Frozen.Contains(owner)) return;
        var root = Ensure(owner);
        if (root == null) return;
        Draw(root, TableCmd.Cards(owner), TableCmd.ShowdownSize(owner), null);
    }

    /// <summary>Call just before the Table is cleared for a Showdown: keeps the scored cards on screen.</summary>
    public static void Freeze(Creature owner, IReadOnlyList<TableCard> cards)
    {
        var root = Ensure(owner);
        if (root == null) return;
        Frozen.Add(owner);
        Draw(root, cards, Math.Max(cards.Count, TableCmd.ShowdownSize(owner)), null);
    }

    /// <summary>Show the scored hand in gold for about a second, then go back to the live Table.</summary>
    public static void ShowdownFlash(Creature owner, IReadOnlyList<TableCard> cards, HandType hand)
    {
        var root = Ensure(owner);
        if (root == null) { Frozen.Remove(owner); return; }
        Draw(root, cards, Math.Max(cards.Count, TableCmd.ShowdownSize(owner)), hand);

        var tween = root.CreateTween();
        tween.TweenProperty(root, "scale", new Vector2(1.12f, 1.12f), 0.12f);
        tween.TweenProperty(root, "scale", Vector2.One, 0.18f);

        root.GetTree().CreateTimer(1.1).Timeout += () =>
        {
            Frozen.Remove(owner);
            if (GodotObject.IsInstanceValid(root)) Refresh(owner);
        };
    }

    // ---------------------------------------------------------------- building

    private static Control? Ensure(Creature owner)
    {
        var creatureNode = owner.GetCreatureNode();
        if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode)) return null;

        var root = creatureNode.GetNodeOrNull<Control>(NodeName);
        if (root == null)
        {
            root = new Control { Name = NodeName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 50 };
            creatureNode.AddChild(root);
        }
        Place(root, creatureNode);
        return root;
    }

    /// <summary>Centre the panel just above the top of the character's hitbox.</summary>
    private static void Place(Control root, Control creatureNode)
    {
        var width = 5 * CardW + 4 * Gap;
        var height = CardH + LineH;
        root.Size = new Vector2(width, height);
        root.PivotOffset = root.Size / 2;

        var bounds = creatureNode.GetNodeOrNull<Control>("%Bounds")
                     ?? FindBounds(creatureNode);
        if (bounds != null)
        {
            var r = bounds.GetGlobalRect();
            root.GlobalPosition = new Vector2(r.Position.X + r.Size.X / 2 - width / 2, r.Position.Y - height - 12);
        }
        else
        {
            root.Position = new Vector2(-width / 2, -height - 480);
        }
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

    private static void Draw(Control root, IReadOnlyList<TableCard> cards, int slots, HandType? showdown)
    {
        foreach (var child in root.GetChildren()) { root.RemoveChild(child); child.QueueFree(); }

        var rowWidth = slots * CardW + (slots - 1) * Gap;
        var x0 = (root.Size.X - rowWidth) / 2;
        for (var i = 0; i < slots; i++)
        {
            var pos = new Vector2(x0 + i * (CardW + Gap), 0);
            if (i < cards.Count) root.AddChild(MiniCard(cards[i], pos, showdown != null));
            else root.AddChild(EmptySlot(pos));
        }

        var text = showdown is { } h
            ? TablePower.HandName(h) + "!"
            : cards.Count == 0 ? $"0/{slots}" : $"{cards.Count}/{slots} · {TablePower.HandName(Poker.Evaluate(cards))}";
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, CardH + 2),
            Size = new Vector2(root.Size.X, LineH),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", showdown != null ? 24 : 20);
        label.AddThemeColorOverride("font_color", showdown != null ? Gold : Cream);
        label.AddThemeColorOverride("font_outline_color", Ink);
        label.AddThemeConstantOverride("outline_size", 6);
        root.AddChild(label);
    }

    private static Control EmptySlot(Vector2 pos)
    {
        var p = new Panel { Position = pos, Size = new Vector2(CardW, CardH), MouseFilter = Control.MouseFilterEnum.Ignore };
        p.AddThemeStyleboxOverride("panel", Box(new Color(0, 0, 0, 0.25f), new Color(Cream, 0.35f), 2));
        return p;
    }

    private static Control MiniCard(TableCard card, Vector2 pos, bool glow)
    {
        var p = new Panel { Position = pos, Size = new Vector2(CardW, CardH), MouseFilter = Control.MouseFilterEnum.Ignore };
        p.AddThemeStyleboxOverride("panel", Box(Cream, glow ? new Color("ffd75e") : Gold, glow ? 4 : 2));

        if (card.Wild)
        {
            p.AddChild(Icon("jester", new Vector2(4, 8), 34));
            return p;
        }

        var red = card.Suit is Suit.Hearts or Suit.Diamonds;
        var rank = new Label
        {
            Text = ShortRank(card.Rank),
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, 1),
            Size = new Vector2(CardW, 26),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rank.AddThemeFontSizeOverride("font_size", 20);
        rank.AddThemeColorOverride("font_color", red ? Red : Ink);
        p.AddChild(rank);
        p.AddChild(Icon(card.Suit.ToString().ToLowerInvariant(), new Vector2((CardW - 24) / 2, 28), 24));
        return p;
    }

    private static TextureRect Icon(string name, Vector2 pos, float size)
    {
        var tex = ResourceLoader.Load<Texture2D>($"{MainFile.ResPath}/images/ui/suit_{name}.png");
        return new TextureRect
        {
            Texture = tex,
            Position = pos,
            Size = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    private static StyleBoxFlat Box(Color bg, Color border, int width) => new()
    {
        BgColor = bg,
        BorderColor = border,
        BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = width,
        CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
    };

    private static string ShortRank(int rank) => rank switch { 11 => "J", 12 => "Q", 13 => "K", 14 => "A", _ => rank.ToString() };
}
