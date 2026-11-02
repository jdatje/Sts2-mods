using Bard.BardCode.Powers;
using Bard.BardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Bard.BardCode.Songs;

/// <summary>Marks a card as a Song card (playing it starts a Song; it never adds a Verse itself).</summary>
public interface ISongCard;

/// <summary>
/// A Song is a Power whose amount is its number of Verses (max 15).
/// Only one Song plays at a time; starting a new one ends the old one and triggers its Finale.
/// Every non-Song card you play adds a Verse. At the start of your turn the Refrain helps ALL players.
/// </summary>
public abstract class SongPower : BardPower
{
    public const int MaxVerses = 15;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Start of your turn. Should help ALL players.</summary>
    public abstract Task Refrain(PlayerChoiceContext ctx);

    /// <summary>When the Song ends. Scales with Verses (Amount).</summary>
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
        await Refrain(choiceContext);
    }
}

public static class SongCmd
{
    public static SongPower? Current(Creature bard) => bard.Powers.OfType<SongPower>().FirstOrDefault();

    /// <summary>Ends the current Song (Finale) and starts a new one with the given Verses.</summary>
    public static async Task Start<T>(PlayerChoiceContext ctx, CardModel card, int verses) where T : SongPower
    {
        var bard = card.Owner.Creature;
        await End(ctx, bard, 1);

        var lute = card.Owner.Relics.OfType<WornLute>().FirstOrDefault();
        if (lute != null && lute.TryUse()) verses += WornLute.BonusVerses;

        await PowerCmd.Apply<T>(ctx, bard, Math.Clamp(verses, 1, SongPower.MaxVerses), bard, card);
    }

    /// <summary>Ends the current Song, triggering its Finale the given number of times.</summary>
    public static async Task<bool> End(PlayerChoiceContext ctx, Creature bard, int finales)
    {
        var song = Current(bard);
        if (song == null) return false;
        for (var i = 0; i < finales; i++)
            await song.Finale(ctx);
        await PowerCmd.Remove(song);
        return true;
    }

    /// <summary>Adds Verses to the current Song, up to the maximum. Does nothing without a Song.</summary>
    public static async Task AddVerses(PlayerChoiceContext ctx, Creature bard, int count)
    {
        var song = Current(bard);
        if (song == null) return;
        var room = SongPower.MaxVerses - song.Verses;
        if (room <= 0) return;
        await PowerCmd.ModifyAmount(ctx, song, Math.Min(count, room), bard, null, silent: true);
    }
}
