using Golem.GolemCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Golem.GolemCode.Parts;

/// <summary>The four body slots. A Golem can have one Part in each.</summary>
public enum PartSlot
{
    Head,
    Arms,
    Legs,
    Core
}

/// <summary>
/// A Part is a Power that occupies one body slot. Installing a new Part in an occupied slot
/// replaces the old one. Everything goes through PowerCmd, so it runs through the game's
/// action queue and stays in sync in multiplayer.
/// </summary>
public abstract class PartPower : GolemPower
{
    public abstract PartSlot Slot { get; }

    /// <summary>Called right after this Part is installed (e.g. to add a linked vanilla power).</summary>
    public virtual Task OnInstalled(PlayerChoiceContext ctx) => Task.CompletedTask;

    /// <summary>Called right before this Part is removed (replaced, Collapse, Overhaul...).</summary>
    public virtual Task OnUninstalled(PlayerChoiceContext ctx) => Task.CompletedTask;
}

/// <summary>Powers that react whenever their owner installs a Part.</summary>
public interface IPartListener
{
    Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part);
}

public static class PartCmd
{
    /// <summary>Removes whatever Part is in the slot, then installs the new one.</summary>
    public static async Task Install<T>(PlayerChoiceContext ctx, CardModel card, decimal amount) where T : PartPower
    {
        var creature = card.Owner.Creature;
        var slot = ModelDb.Power<T>().Slot;

        var existing = creature.Powers.OfType<PartPower>().Where(p => p.Slot == slot).ToList();
        foreach (var old in existing)
            await Uninstall(ctx, old);

        await PowerCmd.Apply<T>(ctx, creature, amount, creature, card);
        var installed = creature.Powers.OfType<T>().FirstOrDefault();
        if (installed == null) return;

        await installed.OnInstalled(ctx);
        foreach (var listener in creature.Powers.OfType<IPartListener>().ToList())
            await listener.AfterPartInstalled(ctx, installed);
    }

    /// <summary>Number of Parts currently installed (0 to 4).</summary>
    public static int Count(Creature? creature) =>
        creature?.Powers.OfType<PartPower>().Count() ?? 0;

    public static bool Has(Creature? creature, PartSlot slot) =>
        creature?.Powers.OfType<PartPower>().Any(p => p.Slot == slot) ?? false;

    /// <summary>Removes all Parts and returns how many were removed.</summary>
    public static async Task<int> RemoveAll(PlayerChoiceContext ctx, Creature creature)
    {
        var parts = creature.Powers.OfType<PartPower>().ToList();
        foreach (var p in parts)
            await Uninstall(ctx, p);
        return parts.Count;
    }

    private static async Task Uninstall(PlayerChoiceContext ctx, PartPower part)
    {
        await part.OnUninstalled(ctx);
        await PowerCmd.Remove(part);
    }
}
