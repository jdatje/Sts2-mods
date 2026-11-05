using Golem.GolemCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Golem.GolemCode.Parts;

/// <summary>The body slots: 1 Head, 1 Core, 2 Arms, 2 Legs (Spare Socket adds a Core slot).</summary>
public enum PartSlot
{
    Head,
    Arms,
    Legs,
    Core
}

/// <summary>
/// A Part is a Power that occupies a body slot. Arms and Legs have two slots, Head and Core one.
/// Installing a Part into a full slot type replaces the oldest Part there. Installing the same Part
/// twice stacks it (the Power's amount adds up) and uses two slots. Everything goes through PowerCmd,
/// so it runs through the game's action queue and stays in sync in multiplayer.
/// </summary>
public abstract class PartPower : GolemPower
{
    public abstract PartSlot Slot { get; }

    /// <summary>One entry per install of this Part: install order and the amount it added.</summary>
    internal readonly List<(long Order, decimal Amount)> Installs = [];

    /// <summary>Installs of this Part (at least 1 while it exists, also after a save/load).</summary>
    internal int InstallCount => Math.Max(1, Installs.Count);

    /// <summary>Called right after this Part is installed (e.g. to add a linked vanilla power).</summary>
    public virtual Task OnInstalled(PlayerChoiceContext ctx) => Task.CompletedTask;

    /// <summary>Called right before this Part is removed completely (replaced, Collapse, Overhaul...).</summary>
    public virtual Task OnUninstalled(PlayerChoiceContext ctx) => Task.CompletedTask;
}

/// <summary>Powers that react whenever their owner installs a Part.</summary>
public interface IPartListener
{
    Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part);
}

public static class PartCmd
{
    /// <summary>Cards that count your Parts count at most this many.</summary>
    public const int CountCap = 4;

    private static long _order;

    public static int Capacity(Creature creature, PartSlot slot) => slot switch
    {
        PartSlot.Arms or PartSlot.Legs => 2,
        PartSlot.Core => creature.Player?.Relics.OfType<Golem.GolemCode.Relics.SpareSocket>().Any() == true ? 2 : 1,
        _ => 1
    };

    /// <summary>Slots used in one slot type.</summary>
    public static int Used(Creature? creature, PartSlot slot) =>
        creature?.Powers.OfType<PartPower>().Where(p => p.Slot == slot).Sum(p => p.InstallCount) ?? 0;

    /// <summary>Installs a Part. A full slot type first loses its oldest Part.</summary>
    public static async Task Install<T>(PlayerChoiceContext ctx, CardModel card, decimal amount) where T : PartPower =>
        await Install<T>(ctx, card.Owner.Creature, amount, card);

    public static async Task Install<T>(PlayerChoiceContext ctx, Creature creature, decimal amount, CardModel? card) where T : PartPower
    {
        var slot = ModelDb.Power<T>().Slot;

        while (Used(creature, slot) >= Capacity(creature, slot))
            if (!await RemoveOldest(ctx, creature, slot)) break;

        await PowerCmd.Apply<T>(ctx, creature, amount, creature, card);
        var installed = creature.Powers.OfType<T>().FirstOrDefault();
        if (installed == null) return;
        installed.Installs.Add((++_order, amount));

        await installed.OnInstalled(ctx);
        foreach (var listener in creature.Powers.OfType<IPartListener>().ToList())
            await listener.AfterPartInstalled(ctx, installed);
        foreach (var relic in creature.Player?.Relics.OfType<IPartListener>().ToList() ?? [])
            await relic.AfterPartInstalled(ctx, installed);
    }

    private static async Task<bool> RemoveOldest(PlayerChoiceContext ctx, Creature creature, PartSlot slot)
    {
        var oldest = creature.Powers.OfType<PartPower>().Where(p => p.Slot == slot)
            .Select(p => (Part: p, Order: p.Installs.Count > 0 ? p.Installs.Min(i => i.Order) : 0L))
            .OrderBy(x => x.Order).FirstOrDefault();
        if (oldest.Part == null) return false;

        var part = oldest.Part;
        if (part.Installs.Count <= 1)
        {
            await Uninstall(ctx, part);
            return true;
        }
        var entry = part.Installs.OrderBy(i => i.Order).First();
        part.Installs.Remove(entry);
        var delta = Math.Min(entry.Amount, part.Amount - 1);
        if (delta > 0) await PowerCmd.ModifyAmount(ctx, part, -delta, creature, null, silent: true);
        return true;
    }

    /// <summary>Parts installed (slots used), counted up to 4 for cards that scale with Parts.</summary>
    public static int Count(Creature? creature) => Math.Min(CountCap, CountAll(creature));

    /// <summary>All Parts installed (slots used), no cap.</summary>
    public static int CountAll(Creature? creature) =>
        creature?.Powers.OfType<PartPower>().Sum(p => p.InstallCount) ?? 0;

    public static bool Has(Creature? creature, PartSlot slot) => Used(creature, slot) > 0;

    /// <summary>True if this slot type still has room.</summary>
    public static bool HasRoom(Creature creature, PartSlot slot) => Used(creature, slot) < Capacity(creature, slot);

    /// <summary>Removes all Parts and returns how many were removed (counted up to 4).</summary>
    public static async Task<int> RemoveAll(PlayerChoiceContext ctx, Creature creature)
    {
        var parts = creature.Powers.OfType<PartPower>().ToList();
        var n = parts.Sum(p => p.InstallCount);
        foreach (var p in parts)
            await Uninstall(ctx, p);
        return Math.Min(CountCap, n);
    }

    private static async Task Uninstall(PlayerChoiceContext ctx, PartPower part)
    {
        await part.OnUninstalled(ctx);
        part.Installs.Clear();
        await PowerCmd.Remove(part);
    }
}
