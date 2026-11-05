using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Songs;

// Refrains grow: +1 per 5 Verses (slow Songs: +1 at 10 Verses). A Song card's upgrade adds 1 to its Refrain.

/// <summary>Refrain: ALL players gain 3 Block (+ the Bard's Dexterity). Finale: deal 2 damage to ALL enemies per Verse.</summary>
public sealed class BattleHymnPower : SongPower
{
    public const int RefrainBlock = 3, FinalePerVerse = 2;
    protected override int DefaultRefrain => RefrainBlock;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        var block = RefrainValue + Dexterity;
        if (block <= 0) return;
        foreach (var ally in Allies)
            await CreatureCmd.GainBlock(ally, block, ValueProp.Unpowered, null);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0)
            await CreatureCmd.Damage(ctx, Enemies, FinalePerVerse * Verses, ValueProp.Unpowered, Owner);
    }
}

/// <summary>Refrain: ALL players gain 2 Vigor. Finale: ALL players gain 2 Block per Verse (+ the Bard's Dexterity).</summary>
public sealed class BalladOfValorPower : SongPower
{
    public const int RefrainVigor = 2, FinalePerVerse = 2;
    protected override int DefaultRefrain => RefrainVigor;

    public override async Task Refrain(PlayerChoiceContext ctx) =>
        await PowerCmd.Apply<VigorPower>(ctx, Allies, RefrainValue, Owner, null);

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var block = FinalePerVerse * Verses + Dexterity;
        if (block <= 0) return;
        foreach (var ally in Allies)
            await CreatureCmd.GainBlock(ally, block, ValueProp.Unpowered, null);
    }
}

/// <summary>Refrain: apply 1 Weak to ALL enemies. Finale: apply 1 Vulnerable to ALL enemies per 3 Verses.</summary>
public sealed class LullabyPower : SongPower
{
    public const int RefrainWeak = 1, VersesPerVulnerable = 3;
    protected override int DefaultRefrain => RefrainWeak;
    protected override bool SlowGrowth => true;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await PowerCmd.Apply<WeakPower>(ctx, Enemies, RefrainValue, Owner, null);
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
    protected override int DefaultRefrain => RefrainDraw;
    protected override bool SlowGrowth => true;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Owner.Player != null) await CardPileCmd.Draw(ctx, RefrainValue, Owner.Player);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var energy = Verses / VersesPerEnergy;
        if (energy > 0 && Owner.Player != null) await PlayerCmd.GainEnergy(energy, Owner.Player);
    }
}

/// <summary>Refrain: deal 2 damage to ALL enemies. Finale: deal 3 damage to ALL enemies per Verse.</summary>
public sealed class RequiemPower : SongPower
{
    public const int RefrainDamage = 2, FinalePerVerse = 3;
    protected override int DefaultRefrain => RefrainDamage;

    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, RefrainValue, ValueProp.Unpowered, Owner);
    }

    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, FinalePerVerse * Verses, ValueProp.Unpowered, Owner);
    }
}
