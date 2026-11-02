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
            await PowerCmd.Remove(old);

        await PowerCmd.Apply<T>(ctx, creature, amount, creature, card);
    }

    /// <summary>Number of Parts currently installed (0 to 4).</summary>
    public static int Count(Creature? creature) =>
        creature?.Powers.OfType<PartPower>().Count() ?? 0;

    /// <summary>Removes all Parts and returns how many were removed.</summary>
    public static async Task<int> RemoveAll(Creature creature)
    {
        var parts = creature.Powers.OfType<PartPower>().ToList();
        foreach (var p in parts)
            await PowerCmd.Remove(p);
        return parts.Count;
    }
}
