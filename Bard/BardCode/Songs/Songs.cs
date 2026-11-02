using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Songs;

/// <summary>Refrain: ALL players gain 3 Block. Finale: deal 2 damage to ALL enemies per Verse.</summary>
public sealed class BattleHymnPower : SongPower
{
    public const int RefrainBlock = 3, FinalePerVerse = 2;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        foreach (var ally in Allies)
            await CreatureCmd.GainBlock(ally, RefrainBlock, ValueProp.Unpowered, null);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0)
            await CreatureCmd.Damage(ctx, Enemies, FinalePerVerse * Verses, ValueProp.Unpowered, Owner);
    }
}

/// <summary>Refrain: ALL players gain 2 Vigor. Finale: ALL players gain 2 Block per Verse.</summary>
public sealed class BalladOfValorPower : SongPower
{
    public const int RefrainVigor = 2, FinalePerVerse = 2;

    public override async Task Refrain(PlayerChoiceContext ctx) =>
        await PowerCmd.Apply<VigorPower>(ctx, Allies, RefrainVigor, Owner, null);

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        foreach (var ally in Allies)
            await CreatureCmd.GainBlock(ally, FinalePerVerse * Verses, ValueProp.Unpowered, null);
    }
}

/// <summary>Refrain: apply 1 Weak to ALL enemies. Finale: apply 1 Vulnerable to ALL enemies per 3 Verses.</summary>
public sealed class LullabyPower : SongPower
{
    public const int RefrainWeak = 1, VersesPerVulnerable = 3;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await PowerCmd.Apply<WeakPower>(ctx, Enemies, RefrainWeak, Owner, null);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var amount = Verses / VersesPerVulnerable;
        if (amount > 0 && Enemies.Count > 0) await PowerCmd.Apply<VulnerablePower>(ctx, Enemies, amount, Owner, null);
    }
}

/// <summary>Refrain: draw 1 card. Finale: gain 1 Energy per 5 Verses.</summary>
public sealed class JigPower : SongPower
{
    public const int RefrainDraw = 1, VersesPerEnergy = 5;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Owner.Player != null) await CardPileCmd.Draw(ctx, RefrainDraw, Owner.Player);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var energy = Verses / VersesPerEnergy;
        if (energy > 0 && Owner.Player != null) await PlayerCmd.GainEnergy(energy, Owner.Player);
    }
}

/// <summary>Refrain: deal 3 damage to ALL enemies. Finale: deal 4 damage to ALL enemies per Verse.</summary>
public sealed class RequiemPower : SongPower
{
    public const int RefrainDamage = 3, FinalePerVerse = 4;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, RefrainDamage, ValueProp.Unpowered, Owner);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, FinalePerVerse * Verses, ValueProp.Unpowered, Owner);
    }
}
