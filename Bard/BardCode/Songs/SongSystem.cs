using Bard.BardCode.Powers;
using Bard.BardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Bard.BardCode.Songs;

/// <summary>Marks a card as a Song card (playing it starts a Song; it never adds a Verse itself).</summary>
public interface ISongCard;

/// <summary>Powers that react to Songs: when one starts, and after each Finale.</summary>
public interface ISongListener
{
    Task AfterSongStarted(PlayerChoiceContext ctx, SongPower song) => Task.CompletedTask;
    Task AfterFinale(PlayerChoiceContext ctx, SongPower song) => Task.CompletedTask;
}

/// <summary>Relics that add Verses to your first Song each combat (Worn Lute, Master Lute). Returns the bonus, or 0.</summary>
public interface IFirstSongBonus
{
    int TryUse();
}

/// <summary>Relics that react to every single Finale trigger (Busker's Hat).</summary>
public interface IFinaleTriggerListener
{
    Task AfterFinaleTrigger(PlayerChoiceContext ctx);
}

/// <summary>Powers that change how Songs behave (Virtuoso, Maestro, Legendary Ballad, Perfect Pitch).</summary>
public interface ISongModifier
{
    int ExtraRefrains => 0;
    int ExtraFinales => 0;
    int ExtraMaxVerses => 0;
    int ExtraStartingVerses => 0;
}

/// <summary>
/// A Song is a Power whose amount is its number of Verses (max 15).
/// Only one Song plays at a time; starting a new one ends the old one and triggers its Finale.
/// Every non-Song card you play adds a Verse. At the start of your turn the Refrain helps ALL players.
/// </summary>
public abstract class SongPower : BardPower
{
    public const int BaseMaxVerses = 15;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Base Refrain value (without upgrade or growth).</summary>
    protected abstract int DefaultRefrain { get; }

    /// <summary>Slow growth: +1 at 10 Verses (draw and debuff Songs). Otherwise +1 per 5 Verses.</summary>
    protected virtual bool SlowGrowth => false;

    /// <summary>Base Refrain from the card that started this Song (its upgrade adds 1); null = DefaultRefrain.</summary>
    internal int? RefrainBase;

    /// <summary>The Refrain's current value: base, +1 per 5 Verses (or +1 at 10 for slow Songs).</summary>
    public int RefrainValue => (RefrainBase ?? DefaultRefrain) + (SlowGrowth ? (Verses >= 10 ? 1 : 0) : Verses / 5);

    /// <summary>The Bard's Dexterity, added to Refrain Block.</summary>
    protected int Dexterity => (int)Owner.Powers.OfType<DexterityPower>().Sum(p => p.Amount);

    /// <summary>Start of your turn. Should help ALL players.</summary>
    public abstract Task Refrain(PlayerChoiceContext ctx);

    /// <summary>When the Song ends (or Medley / Curtain Call). Scales with Verses (Amount).</summary>
    public abstract Task Finale(PlayerChoiceContext ctx);

    public int Verses => (int)Amount;

    protected IReadOnlyList<Creature> Allies =>
        CombatState?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? (IReadOnlyList<Creature>)[];

    protected IReadOnlyList<Creature> Enemies =>
        CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card is ISongCard) return;
        await SongCmd.AddVerses(choiceContext, Owner, 1);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        var times = 1 + Owner.Powers.OfType<ISongModifier>().Sum(m => m.ExtraRefrains);
        if (Verses >= GoldenStrings.MinVerses && Owner.Player?.Relics.OfType<GoldenStrings>().Any() == true) times++;
        for (var i = 0; i < times; i++)
            await Refrain(choiceContext);
    }
}

public static class SongCmd
{
    public static SongPower? Current(Creature bard) => bard.Powers.OfType<SongPower>().FirstOrDefault();

    public static int Verses(Creature bard) => Current(bard)?.Verses ?? 0;

    public static int MaxVerses(Creature bard) =>
        SongPower.BaseMaxVerses + bard.Powers.OfType<ISongModifier>().Sum(m => m.ExtraMaxVerses)
        + (bard.Player?.Relics.OfType<TuningFork>().Any() == true ? TuningFork.ExtraVerses : 0);

    /// <summary>Ends the current Song (Finale) and starts a new one with the given Verses.</summary>
    public static async Task Start<T>(PlayerChoiceContext ctx, CardModel card, int verses) where T : SongPower
    {
        var bard = card.Owner.Creature;
        await End(ctx, bard, 1);

        foreach (var lute in card.Owner.Relics.OfType<IFirstSongBonus>())
            verses += lute.TryUse();
        verses += bard.Powers.OfType<ISongModifier>().Sum(m => m.ExtraStartingVerses);

        await PowerCmd.Apply<T>(ctx, bard, Math.Clamp(verses, 1, MaxVerses(bard)), bard, card);
        var song = Current(bard);
        if (song == null) return;
        if (card is ISongCard && card.DynamicVars.TryGetValue("Refrain", out var refrain))
            song.RefrainBase = refrain.IntValue;
        foreach (var l in bard.Powers.OfType<ISongListener>().ToList())
            await l.AfterSongStarted(ctx, song);
        foreach (var l in card.Owner.Relics.OfType<ISongListener>().ToList())
            await l.AfterSongStarted(ctx, song);
    }

    /// <summary>Triggers the current Song's Finale without ending it. Returns false without a Song.</summary>
    public static async Task<bool> Finale(PlayerChoiceContext ctx, Creature bard, int times)
    {
        var song = Current(bard);
        if (song == null) return false;
        var total = times * (1 + bard.Powers.OfType<ISongModifier>().Sum(m => m.ExtraFinales));
        for (var i = 0; i < total; i++)
        {
            await song.Finale(ctx);
            foreach (var r in bard.Player?.Relics.OfType<IFinaleTriggerListener>().ToList() ?? [])
                await r.AfterFinaleTrigger(ctx);
        }
        foreach (var l in bard.Powers.OfType<ISongListener>().ToList())
            await l.AfterFinale(ctx, song);
        return true;
    }

    /// <summary>Ends the current Song, triggering its Finale the given number of times.</summary>
    public static async Task<bool> End(PlayerChoiceContext ctx, Creature bard, int finales)
    {
        var song = Current(bard);
        if (song == null) return false;
        await Finale(ctx, bard, finales);
        await PowerCmd.Remove(song);
        return true;
    }

    /// <summary>Adds Verses to the current Song, up to the maximum. Does nothing without a Song.</summary>
    public static async Task AddVerses(PlayerChoiceContext ctx, Creature bard, int count)
    {
        var song = Current(bard);
        if (song == null) return;
        var room = MaxVerses(bard) - song.Verses;
        if (room <= 0 || count <= 0) return;
        await PowerCmd.ModifyAmount(ctx, song, Math.Min(count, room), bard, null, silent: true);
    }

    /// <summary>Sets the current Song's Verses to an exact number (1..max).</summary>
    public static async Task SetVerses(PlayerChoiceContext ctx, Creature bard, int verses)
    {
        var song = Current(bard);
        if (song == null) return;
        var target = Math.Clamp(verses, 1, MaxVerses(bard));
        if (target != song.Verses)
            await PowerCmd.ModifyAmount(ctx, song, target - song.Verses, bard, null, silent: true);
    }
}
