using Bard.BardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Songs;

// ========================= New Songs =========================

/// <summary>Refrain: apply 1 Vulnerable to ALL enemies. Finale: ALL players gain 1 Strength per 5 Verses.</summary>
public sealed class WarDrumsPower : SongPower
{
    protected override int DefaultRefrain => 1;
    protected override bool SlowGrowth => true;
    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await PowerCmd.Apply<VulnerablePower>(ctx, Enemies, RefrainValue, Owner, null);
    }
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var str = Verses / 5;
        if (str > 0) await PowerCmd.Apply<StrengthPower>(ctx, Allies, str, Owner, null);
    }
}

/// <summary>Refrain: ALL players gain 2 Block (+ the Bard's Dexterity). Finale: ALL players heal 1 HP per 3 Verses.</summary>
public sealed class SeaShantyPower : SongPower
{
    protected override int DefaultRefrain => 2;
    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        var block = RefrainValue + Dexterity;
        if (block <= 0) return;
        foreach (var a in Allies) await CreatureCmd.GainBlock(a, block, ValueProp.Unpowered, null);
    }
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var heal = Verses / 3;
        if (heal > 0) foreach (var a in Allies) await CreatureCmd.Heal(a, heal);
    }
}

/// <summary>Refrain: ALL enemies lose 2 HP. Finale: apply 2 Doom per Verse to ALL enemies.</summary>
public sealed class DirgePower : SongPower
{
    protected override int DefaultRefrain => 2;
    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, RefrainValue, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
    }
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await PowerCmd.Apply<DoomPower>(ctx, Enemies, 2 * Verses, Owner, null);
    }
}

/// <summary>Refrain: ALL players draw 1 card. Finale: ALL players gain 1 Energy per 5 Verses.</summary>
public sealed class OdeToJoyPower : SongPower
{
    protected override int DefaultRefrain => 1;
    protected override bool SlowGrowth => true;
    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        foreach (var a in Allies) if (a.Player != null) await CardPileCmd.Draw(ctx, RefrainValue, a.Player);
    }
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var e = Verses / 5;
        if (e > 0) foreach (var a in Allies) if (a.Player != null) await PlayerCmd.GainEnergy(e, a.Player);
    }
}

/// <summary>Refrain: ALL players gain 1 Plating. Finale: ALL players gain 1 Dexterity per 5 Verses.</summary>
public sealed class AnthemPower : SongPower
{
    protected override int DefaultRefrain => 1;
    protected override bool SlowGrowth => true;
    public override async Task Refrain(PlayerChoiceContext ctx) =>
        await PowerCmd.Apply<PlatingPower>(ctx, Allies, RefrainValue, Owner, null);
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        var dex = Verses / 5;
        if (dex > 0) await PowerCmd.Apply<DexterityPower>(ctx, Allies, dex, Owner, null);
    }
}

/// <summary>Refrain: apply 2 Weak to ALL enemies. Finale: deal 3 damage per Verse to ALL enemies.</summary>
public sealed class SirenSongPower : SongPower
{
    protected override int DefaultRefrain => 2;
    protected override bool SlowGrowth => true;
    public override async Task Refrain(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await PowerCmd.Apply<WeakPower>(ctx, Enemies, RefrainValue, Owner, null);
    }
    public override async Task Finale(PlayerChoiceContext ctx)
    {
        if (Enemies.Count > 0) await CreatureCmd.Damage(ctx, Enemies, 3 * Verses, ValueProp.Unpowered, Owner);
    }
}

// ========================= Bard powers =========================

public abstract class SimpleBardPower : BardPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected IReadOnlyList<Creature> Allies => CombatState?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? (IReadOnlyList<Creature>)[];
}

/// <summary>Whenever you start a Song, gain X Energy.</summary>
public sealed class ImprovisePower : SimpleBardPower, ISongListener
{
    public async Task AfterSongStarted(PlayerChoiceContext ctx, SongPower song)
    {
        if (Owner.Player == null) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}

/// <summary>Whenever a Finale triggers, draw X cards.</summary>
public sealed class StagePresencePower : SimpleBardPower, ISongListener
{
    public async Task AfterFinale(PlayerChoiceContext ctx, SongPower song)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

/// <summary>Co-op. Whenever another player plays an Attack, add X Verses.</summary>
public sealed class AccompanimentPower : SimpleBardPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player || play.Card.Type != CardType.Attack) return;
        if (SongCmd.Current(Owner) == null) return;
        await SongCmd.AddVerses(ctx, Owner, (int)Amount);
    }
}

/// <summary>At the start of your turn, gain X Block for every 2 Verses.</summary>
public sealed class TempoPower : SimpleBardPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player) return;
        var block = Amount * (SongCmd.Verses(Owner) / 2);
        if (block <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}

/// <summary>Your Songs start with X extra Verses.</summary>
public sealed class PerfectPitchPower : SimpleBardPower, ISongModifier
{
    public int ExtraStartingVerses => (int)Amount;
}

/// <summary>Your Refrains trigger X extra times.</summary>
public sealed class VirtuosoPower : SimpleBardPower, ISongModifier
{
    public int ExtraRefrains => (int)Amount;
}

/// <summary>Your Finales trigger X extra times.</summary>
public sealed class MaestroPower : SimpleBardPower, ISongModifier
{
    public int ExtraFinales => (int)Amount;
}

/// <summary>Your Songs can have X more Verses (cap 15 + X).</summary>
public sealed class LegendaryBalladPower : SimpleBardPower, ISongModifier
{
    public int ExtraMaxVerses => (int)Amount;
}

/// <summary>Whenever a Finale triggers, ALL players gain X Strength.</summary>
public sealed class BardicInspirationPower : SimpleBardPower, ISongListener
{
    public async Task AfterFinale(PlayerChoiceContext ctx, SongPower song)
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(ctx, Allies, Amount, Owner, null);
    }
}

/// <summary>Whenever you start a Song, gain X Block.</summary>
public sealed class GrandStagePower : SimpleBardPower, ISongListener
{
    public async Task AfterSongStarted(PlayerChoiceContext ctx, SongPower song)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>At the start of your turn, draw X cards for every 5 Verses.</summary>
public sealed class MusePower : SimpleBardPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player) return;
        var n = Amount * (SongCmd.Verses(Owner) / 5);
        if (n <= 0) return;
        Flash();
        await CardPileCmd.Draw(ctx, n, player);
    }
}
