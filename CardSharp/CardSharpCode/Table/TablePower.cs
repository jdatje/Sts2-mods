using BaseLib.Extensions;
using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Powers;
using CardSharp.CardSharpCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
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

    /// <summary>Stack the Deck / Flush Out: the next N cards count as this suit.</summary>
    public int SuitOverrideCount;
    public Suit SuitOverride;

    public override int DisplayAmount => Cards.Count;

    /// <summary>Hover text: which cards are on the Table and the best hand so far.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar("TableCards", "nothing yet"), new StringVar("TableHand", "")];

    public void Changed()
    {
        if (DynamicVars.TryGetValue("TableCards", out var c) && c is StringVar cards)
            cards.StringValue = Cards.Count == 0 ? "nothing yet" : string.Join(", ", Cards.Select(Label));
        if (DynamicVars.TryGetValue("TableHand", out var h) && h is StringVar hand)
            hand.StringValue = Cards.Count == 0 ? "" : $" ({HandName(Poker.Evaluate(Cards))} so far)";
        InvokeDisplayAmountChanged();
        TableOverlay.Refresh(Owner);
    }

    /// <summary>The Table never carries over between fights.</summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        Reset();
        TableOverlay.Hide(Owner);
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        Reset();
        return Task.CompletedTask;
    }

    private void Reset()
    {
        Cards.Clear();
        SuitOverrideCount = 0;
        if (DynamicVars.TryGetValue("TableCards", out var c) && c is StringVar cards) cards.StringValue = "nothing yet";
        if (DynamicVars.TryGetValue("TableHand", out var h) && h is StringVar hand) hand.StringValue = "";
        InvokeDisplayAmountChanged();
    }

    public static string Label(TableCard c) => c.Wild ? "Joker" : $"{ShortRank(c.Rank)} {c.Suit}";

    private static string ShortRank(int rank) => rank switch { 11 => "J", 12 => "Q", 13 => "K", 14 => "A", _ => rank.ToString() };

    public static string HandName(HandType hand) => hand switch
    {
        HandType.HighCard => "High card",
        HandType.TwoPair => "Two pair",
        HandType.ThreeOfAKind => "Three of a kind",
        HandType.FullHouse => "Full house",
        HandType.FourOfAKind => "Four of a kind",
        HandType.StraightFlush => "Straight flush",
        _ => hand.ToString()
    };
}

/// <summary>Powers that react after each Showdown of their owner.</summary>
public interface IShowdownListener
{
    Task AfterShowdown(PlayerChoiceContext ctx, HandType hand);
}

/// <summary>Powers that change Showdown damage (percent bonus, e.g. 50 = +50%) or the hand tier.</summary>
public interface IShowdownModifier
{
    int DamageBonusPercent { get; }
    int TierBonus { get; }
}

public static class TableCmd
{
    public static TablePower? Find(Creature owner) => owner.Powers.OfType<TablePower>().FirstOrDefault();

    public static IReadOnlyList<TableCard> Cards(Creature owner) =>
        Find(owner)?.Cards ?? (IReadOnlyList<TableCard>)[];

    public static int Count(Creature owner) => Cards(owner).Count;

    public static bool HasAtLeast(Creature owner, HandType hand) =>
        Count(owner) > 0 && Poker.Evaluate(Cards(owner)) >= hand;

    public static async Task<TablePower> Get(PlayerChoiceContext ctx, Creature owner)
    {
        var table = Find(owner);
        if (table != null) return table;
        await PowerCmd.Apply<TablePower>(ctx, owner, 1, owner, null, silent: true);
        return owner.Powers.OfType<TablePower>().First();
    }

    public static int ShowdownSize(Creature owner) =>
        owner.Powers.OfType<CardSharkPower>().Any() ? 4 : 5;

    /// <summary>Lay a card on the Table; triggers a Showdown when the Table is full.</summary>
    public static async Task Lay(PlayerChoiceContext ctx, CardSharpCard card)
    {
        var owner = card.Owner.Creature;
        var table = await Get(ctx, owner);

        if (card.IsWild)
        {
            table.Cards.Add(new TableCard(0, Suit.Spades, true));
        }
        else
        {
            var suit = card.Suit;
            if (table.SuitOverrideCount > 0)
            {
                suit = table.SuitOverride;
                table.SuitOverrideCount--;
            }
            table.Cards.Add(new TableCard(card.EnsureRank(), suit));
        }
        table.Changed();

        if (table.Cards.Count >= ShowdownSize(owner))
            await Showdown(ctx, card);
    }

    /// <summary>Put cards back on the Table without triggering anything (used by Final Hand).</summary>
    public static async Task Restore(PlayerChoiceContext ctx, Creature owner, IEnumerable<TableCard> cards)
    {
        var table = await Get(ctx, owner);
        table.Cards.AddRange(cards);
        table.Changed();
    }

    /// <summary>Remove all cards from the Table without scoring. Returns how many were removed.</summary>
    public static int Clear(Creature owner)
    {
        var table = Find(owner);
        if (table == null) return 0;
        var n = table.Cards.Count;
        table.Cards.Clear();
        table.Changed();
        return n;
    }

    /// <summary>Score the Table, apply the effect, clear the Table. Returns the hand (or null if the Table was empty).</summary>
    public static async Task<HandType?> Showdown(PlayerChoiceContext ctx, CardSharpCard source, int extraTiers = 0)
    {
        var player = source.Owner;
        var owner = player.Creature;
        var table = Find(owner);
        if (table == null || table.Cards.Count == 0) return null;

        var cards = table.Cards.ToList();
        TableOverlay.Freeze(owner, cards);
        table.Cards.Clear();
        table.Changed();

        var modifiers = owner.Powers.OfType<IShowdownModifier>().ToList();
        var hand = Poker.Evaluate(cards);
        var tiers = extraTiers + modifiers.Sum(m => m.TierBonus);
        var marked = player.Relics.OfType<MarkedDeck>().FirstOrDefault();
        if (marked != null && marked.TryUse()) tiers++;
        hand = Poker.TiersHigher(hand, tiers);
        TableOverlay.ShowdownFlash(owner, cards, hand);

        TalkCmd.Play(new LocString("powers", "CARDSHARP-SHOWDOWN." + hand), owner, VfxColor.Gold, VfxDuration.Short);

        var combat = source.CombatState;
        if (combat == null) return hand;
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
                    if (enemies.Count > 0) await PowerCmd.Apply<WeakPower>(ctx, enemies, 2, owner, null);
                    break;
            }
        }

        var bonus = modifiers.Sum(m => m.DamageBonusPercent);
        damage = damage * (100 + bonus) / 100;

        if (damage > 0 && enemies.Count > 0)
            await CreatureCmd.Damage(ctx, enemies, damage, ValueProp.Unpowered, owner);
        if (block > 0)
            await CreatureCmd.GainBlock(owner, block, ValueProp.Unpowered, null);
        if (draw > 0)
            await CardPileCmd.Draw(ctx, draw, player);
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, player);

        foreach (var listener in owner.Powers.OfType<IShowdownListener>().ToList())
            await listener.AfterShowdown(ctx, hand);

        return hand;
    }
}
