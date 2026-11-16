using CardSharp.CardSharpCode.Powers;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardSharp.CardSharpCode.Cards;

// Cards for the Flush and Straights archetypes (16 Nov 2026).
// A card's Effect runs before it is laid on the Table, so "the last card on the Table" is the one before it.

/// <summary>Common. Deal 7 damage, +2 for each Spade on the Table.</summary>
public sealed class SpadeWork : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public SpadeWork() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, 2, (card, _) => TableCmd.Cards(card.Owner.Creature).Count(c => !c.Wild && c.Suit == Suit.Spades),
            upgrade: 3, bonusUpgrade: 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Common. Gain 8 Block. If all cards on the Table share a suit, gain 5 more.</summary>
public sealed class ColourMatch : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public ColourMatch() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(8, 3); WithVar("Extra", 5, 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        var table = TableCmd.Cards(Owner.Creature).Where(c => !c.Wild).ToList();
        if (table.Count > 0 && table.All(c => c.Suit == table[0].Suit))
            await CreatureCmd.GainBlock(Owner.Creature, V("Extra"), ValueProp.Move, play);
    }
}

/// <summary>Common. Deal 7 damage. If this card's rank is one higher than the last card on the Table, deal 7 more.</summary>
public sealed class NextCard : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public NextCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 2); WithVar("Bonus", 7, 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        var last = TableCmd.Cards(Owner.Creature).LastOrDefault();
        var next = last.Rank >= Poker.MaxRank ? Poker.MinRank : last.Rank + 1;
        if (last.Wild || (last.Rank > 0 && EnsureRank() == next))
            await CreatureCmd.Damage(ctx, play.Target!, V("Bonus"), ValueProp.Move, Owner.Creature, this);
    }
}

/// <summary>Uncommon. 2 random cards in your Hand get the next ranks after the last card on the Table. Draw 1 card.</summary>
public sealed class Ladder : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public const int Count = 2;
    public Ladder() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var last = TableCmd.Cards(Owner.Creature).LastOrDefault(t => !t.Wild);
        if (last.Rank > 0)
        {
            var hand = Owner.PlayerCombatState!.Hand.Cards.OfType<CardSharpCard>().Where(c => c != this && !c.IsWild).ToList();
            var rank = last.Rank;
            for (var i = 0; i < Count && hand.Count > 0; i++)
            {
                var pick = hand[Owner.RunState.Rng.CombatCardGeneration.NextInt(0, hand.Count)];
                hand.Remove(pick);
                rank = rank >= Poker.MaxRank ? Poker.MinRank : rank + 1;
                pick.SetRank(rank);
            }
        }
        await DrawCards(ctx);
    }
}

/// <summary>Uncommon. Whenever you play a card of the same suit as the last card on the Table, draw 1 card (max 2 per turn).</summary>
public sealed class Monochrome() : SharpPowerCard<MonochromePower>(Suit.Diamonds, 1, CardRarity.Uncommon, 1, 0, -1);

/// <summary>Rare. Deal 6 damage to ALL enemies for each card in the longest run of consecutive ranks on the Table.</summary>
public sealed class Broadway : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Broadway() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCalculatedDamage(0, 6, (card, _) => LongestRun(TableCmd.Cards(card.Owner.Creature)), bonusUpgrade: 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);

    /// <summary>Longest run of consecutive ranks (Ace also counts low); each Joker extends the run by 1, up to 5.</summary>
    public static int LongestRun(IReadOnlyList<TableCard> cards)
    {
        var ranks = cards.Where(c => !c.Wild).Select(c => c.Rank).Distinct().ToList();
        if (ranks.Contains(Poker.MaxRank)) ranks.Add(1);
        ranks.Sort();
        int best = 0, run = 0, prev = -10;
        foreach (var r in ranks)
        {
            run = r == prev + 1 ? run + 1 : 1;
            best = Math.Max(best, run);
            prev = r;
        }
        return Math.Min(5, best + cards.Count(c => c.Wild));
    }
}
