using BaseLib.Extensions;
using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Powers;
using CardSharp.CardSharpCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardSharp.CardSharpCode.Table;

/// <summary>
/// The Table: every Card Sharp card you play is laid here with its rank and suit.
/// When it holds 5 cards (4 with Card Shark) a Showdown happens and the Table is cleared.
/// The amount shown on the icon is the number of cards on the Table.
/// </summary>
public sealed class TablePower : CardSharpPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public readonly List<TableCard> Cards = [];

    /// <summary>Stack the Deck: the next N cards count as this suit.</summary>
    public int SuitOverrideCount;
    public Suit SuitOverride;

    public override int DisplayAmount => Cards.Count;

    public void Changed() => InvokeDisplayAmountChanged();
}

public static class TableCmd
{
    public static async Task<TablePower> Get(PlayerChoiceContext ctx, MegaCrit.Sts2.Core.Entities.Creatures.Creature owner)
    {
        var table = owner.Powers.OfType<TablePower>().FirstOrDefault();
        if (table != null) return table;
        await PowerCmd.Apply<TablePower>(ctx, owner, 1, owner, null, silent: true);
        return owner.Powers.OfType<TablePower>().First();
    }

    public static int ShowdownSize(MegaCrit.Sts2.Core.Entities.Creatures.Creature owner) =>
        owner.Powers.OfType<CardSharkPower>().Any() ? 4 : 5;

    /// <summary>Lay a card on the Table; triggers a Showdown when the Table is full.</summary>
    public static async Task Lay(PlayerChoiceContext ctx, CardSharpCard card)
    {
        var owner = card.Owner.Creature;
        var table = await Get(ctx, owner);

        var suit = card.Suit;
        if (table.SuitOverrideCount > 0)
        {
            suit = table.SuitOverride;
            table.SuitOverrideCount--;
        }

        table.Cards.Add(new TableCard(card.EnsureRank(), suit));
        table.Changed();

        if (table.Cards.Count >= ShowdownSize(owner))
            await Showdown(ctx, card);
    }

    /// <summary>Remove all cards from the Table without scoring. Returns how many were removed.</summary>
    public static int Clear(MegaCrit.Sts2.Core.Entities.Creatures.Creature owner)
    {
        var table = owner.Powers.OfType<TablePower>().FirstOrDefault();
        if (table == null) return 0;
        var n = table.Cards.Count;
        table.Cards.Clear();
        table.Changed();
        return n;
    }

    /// <summary>Score the Table, apply the effect, clear the Table.</summary>
    public static async Task Showdown(PlayerChoiceContext ctx, CardSharpCard source)
    {
        var player = source.Owner;
        var owner = player.Creature;
        var table = owner.Powers.OfType<TablePower>().FirstOrDefault();
        if (table == null || table.Cards.Count == 0) return;

        var cards = table.Cards.ToList();
        table.Cards.Clear();
        table.Changed();

        var hand = Poker.Evaluate(cards);
        var marked = player.Relics.OfType<MarkedDeck>().FirstOrDefault();
        if (marked != null && marked.TryUse()) hand = Poker.OneTierHigher(hand);

        TalkCmd.Play(new LocString("powers", "CARDSHARP-SHOWDOWN." + hand), owner, VfxColor.Gold, VfxDuration.Short);

        var combat = source.CombatState;
        if (combat == null) return;
        var enemies = combat.HittableEnemies;

        var (damage, block, draw, energy) = hand switch
        {
            HandType.HighCard => (5, 0, 0, 0),
            HandType.Pair => (9, 0, 0, 0),
            HandType.TwoPair => (9, 8, 0, 0),
            HandType.ThreeOfAKind => (16, 0, 0, 0),
            HandType.Straight => (12, 0, 2, 0),
            HandType.Flush => (14, 0, 0, 0),
            HandType.FullHouse => (22, 12, 0, 0),
            HandType.FourOfAKind => (32, 0, 0, 0),
            HandType.StraightFlush => (45, 0, 0, 2),
            _ => (0, 0, 0, 0)
        };

        if (hand == HandType.Flush)
        {
            switch (Poker.MainSuit(cards))
            {
                case Suit.Spades: damage += 6; break;
                case Suit.Hearts: block += 10; break;
                case Suit.Diamonds: draw += 2; break;
                case Suit.Clubs:
                    await PowerCmd.Apply<WeakPower>(ctx, enemies, 2, owner, null);
                    break;
            }
        }

        if (damage > 0 && enemies.Count > 0)
            await CreatureCmd.Damage(ctx, enemies, damage, ValueProp.Unpowered, owner);
        if (block > 0)
            await CreatureCmd.GainBlock(owner, block, ValueProp.Unpowered, null);
        if (draw > 0)
            await CardPileCmd.Draw(ctx, draw, player);
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, player);
    }
}
