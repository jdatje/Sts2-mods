using Godot;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Golem.GolemCode.Visuals;

/// <summary>
/// Always-visible line above the Golem's head showing its Part slots:
/// "Head ● · Core ○ · Arms ●○ · Legs ●●" (filled = Part installed, empty = free slot).
/// Built from plain Godot nodes attached to the creature's NCreature node, so it moves with the Golem
/// and every player in co-op sees it.
/// </summary>
public static class SlotOverlay
{
    private const string NodeName = "GolemSlotOverlay";
    private const float Width = 460, Height = 34;
    private static readonly Color Gold = new("d4a840");
    private static readonly Color Cream = new("f6efdc");
    private static readonly Color Faint = new(1f, 1f, 1f, 0.35f);

    private static readonly (PartSlot Slot, string Name)[] Slots =
        [(PartSlot.Head, "Head"), (PartSlot.Core, "Core"), (PartSlot.Arms, "Arms"), (PartSlot.Legs, "Legs")];

    public static void Refresh(Creature owner)
    {
        try
        {
            var root = Ensure(owner);
            if (root == null) return;
            foreach (var child in root.GetChildren()) { root.RemoveChild(child); child.QueueFree(); }

            var row = new HBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Size = root.Size,
            };
            row.AddThemeConstantOverride("separation", 6);
            root.AddChild(row);

            for (var i = 0; i < Slots.Length; i++)
            {
                var (slot, name) = Slots[i];
                if (i > 0) row.AddChild(Text("·", Faint, 20));
                row.AddChild(Text(name, Gold, 20));
                var used = PartCmd.Used(owner, slot);
                var cap = PartCmd.Capacity(owner, slot);
                for (var d = 0; d < cap; d++)
                    row.AddChild(Text(d < used ? "●" : "○", d < used ? Cream : Faint, 20));
            }
        }
        catch (Exception)
        {
            // purely visual; never let it break combat
        }
    }

    public static void Hide(Creature owner)
    {
        var node = owner.GetCreatureNode();
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        node.GetNodeOrNull<Control>(NodeName)?.QueueFree();
    }

    private static Label Text(string text, Color color, int size)
    {
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        l.AddThemeConstantOverride("outline_size", 6);
        l.AddThemeFontSizeOverride("font_size", size);
        return l;
    }

    private static Control? Ensure(Creature owner)
    {
        var node = owner.GetCreatureNode();
        if (node == null || !GodotObject.IsInstanceValid(node)) return null;
        var root = node.GetNodeOrNull<Control>(NodeName);
        if (root == null)
        {
            root = new Control { Name = NodeName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 50 };
            node.AddChild(root);
        }
        root.Size = new Vector2(Width, Height);
        var bounds = node.GetNodeOrNull<Control>("%Bounds") ?? FindBounds(node);
        if (bounds != null)
        {
            var r = bounds.GetGlobalRect();
            root.GlobalPosition = new Vector2(r.Position.X + r.Size.X / 2 - Width / 2, r.Position.Y - Height - 12);
        }
        else root.Position = new Vector2(-Width / 2, -Height - 480);
        return root;
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
