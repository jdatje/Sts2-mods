using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Powers;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardSharp.CardSharpCode.Cards;

/// <summary>Base for simple power cards: applies power T to yourself (and is laid on the Table like any card).</summary>
public abstract class SharpPowerCard<T> : CardSharpCard where T : MegaCrit.Sts2.Core.Models.PowerModel
{
    private readonly Suit _suit;
    public override Suit Suit => _suit;

    protected SharpPowerCard(Suit suit, int cost, CardRarity rarity, int amount, int upgrade = 0, int costUpgrade = 0)
        : base(cost, CardType.Power, rarity, TargetType.Self)
    {
        _suit = suit;
        WithPower<T>(amount, upgrade);
        if (costUpgrade != 0) WithCostUpgradeBy(costUpgrade);
    }

    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Buff<T>(ctx);
}

static class RankOf
{
    /// <summary>The rank of a card, for calculated vars (0 outside combat).</summary>
    public static int Card(CardModel card) => card is CardSharpCard c && !c.IsWild ? c.Rank : 0;
}

// =====================================================================
//  TOKEN
// =====================================================================

/// <summary>Token. Draw 1 card. Counts as any rank and suit on the Table. Exhaust.</summary>
public sealed class Joker : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public override bool IsWild => true;
    public Joker() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithCards(1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await DrawCards(ctx);
}

// =====================================================================
//  COMMON
// =====================================================================

/// <summary>Deal damage equal to this card's rank.</summary>
public sealed class HoleCard : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public HoleCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0, 1, (card, _) => RankOf.Card(card), upgrade: 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Deal damage equal to 16 minus this card's rank.</summary>
public sealed class Lowball : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Lowball() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(0, 1, (card, _) => RankOf.Card(card) == 0 ? 0 : 16 - RankOf.Card(card), upgrade: 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Gain 6 Block. If the Table has a Pair or better, gain 6 more.</summary>
public sealed class PocketPair : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public PocketPair() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 2); WithVar("Extra", 6, 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (TableCmd.HasAtLeast(Owner.Creature, HandType.Pair)) await GainBlock(V("Extra"), play);
    }
}

/// <summary>Gain 4 Block, plus 3 for each card on the Table.</summary>
public sealed class ChipStack : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public ChipStack() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(4, 3, (card, _) => TableCmd.Count(card.Owner.Creature), upgrade: 2, bonusUpgrade: 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Draw 2 cards, then discard 1 card.</summary>
public sealed class AnteUp : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public AnteUp() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(2, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await DrawCards(ctx);
        var discard = await CardSelectCmd.FromHandForDiscard(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c != this, this);
        await CardCmd.Discard(ctx, discard);
    }
}

/// <summary>Deal 6 damage. If a card on the Table has the same rank, deal 6 more.</summary>
public sealed class Snap : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Snap() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2); WithVar("Repeat", 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var match = TableCmd.Cards(Owner.Creature).Any(t => t.Wild || t.Rank == EnsureRank());
        await Hit(ctx, play, match ? V("Repeat") : 1);
    }
}

/// <summary>Deal 6 damage to ALL enemies.</summary>
public sealed class Flick : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Flick() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { WithDamage(6, 3); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Apply 1 Weak to ALL enemies. Draw 1 card.</summary>
public sealed class BadBeat : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public BadBeat() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1); WithCards(1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await DebuffAll<WeakPower>(ctx); await DrawCards(ctx); }
}

/// <summary>Draw 1 card for each different suit on the Table.</summary>
public sealed class ReadTheTable : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public ReadTheTable() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Bonus", 0, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var suits = TableCmd.Cards(Owner.Creature).Where(t => !t.Wild).Select(t => t.Suit).Distinct().Count();
        var n = suits + V("Bonus");
        if (n > 0) await CardPileCmd.Draw(ctx, n, Owner);
    }
}

/// <summary>Deal 8 damage. Apply 1 Vulnerable.</summary>
public sealed class CheapShot : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public CheapShot() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 3); WithPower<VulnerablePower>(1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<VulnerablePower>(ctx, play); }
}

/// <summary>Gain 7 Block. Draw 1 card.</summary>
public sealed class Hedge : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public Hedge() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { WithBlock(7, 3); WithCards(1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await DrawCards(ctx); }
}

/// <summary>Apply 2 Weak. Draw 1 card.</summary>
public sealed class Intimidate : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public Intimidate() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(2, 1); WithCards(1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Debuff<WeakPower>(ctx, play); await DrawCards(ctx); }
}

// =====================================================================
//  UNCOMMON — powers
// =====================================================================

public sealed class LuckyStreak() : SharpPowerCard<LuckyStreakPower>(Suit.Diamonds, 1, CardRarity.Uncommon, 2, 1);
public sealed class Croupier() : SharpPowerCard<CroupierPower>(Suit.Hearts, 1, CardRarity.Uncommon, 8, 3);
public sealed class HighStakes() : SharpPowerCard<HighStakesPower>(Suit.Spades, 2, CardRarity.Uncommon, 50, 0, -1);
public sealed class PokerNight() : SharpPowerCard<PokerNightPower>(Suit.Clubs, 1, CardRarity.Uncommon, 1, 0, -1);
public sealed class LoadedDeck() : SharpPowerCard<LoadedDeckPower>(Suit.Diamonds, 1, CardRarity.Uncommon, 1, 0, -1);
public sealed class Bluffer() : SharpPowerCard<BlufferPower>(Suit.Hearts, 1, CardRarity.Uncommon, 2, 1);
public sealed class SidePot() : SharpPowerCard<SidePotPower>(Suit.Diamonds, 1, CardRarity.Uncommon, 2, 1);
public sealed class PitBoss() : SharpPowerCard<PitBossPower>(Suit.Spades, 2, CardRarity.Uncommon, 3, 1);

// =====================================================================
//  UNCOMMON — skills and attacks
// =====================================================================

/// <summary>Add a Joker into your hand.</summary>
public sealed class WildCard : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public WildCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1); WithTip(typeof(Joker));
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CardPileCmd.AddToCombatAndPreview<Joker>(Owner.Creature, PileType.Hand, 1, Owner);
}

/// <summary>Deal 4 damage, plus 5 for each card on the Table.</summary>
public sealed class Reraise : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Reraise() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(4, 5, (card, _) => TableCmd.Count(card.Owner.Creature), upgrade: 4, bonusUpgrade: 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Deal 14 damage. If this card is a Jack, Queen, King or Ace, deal 8 more.</summary>
public sealed class CourtCard : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public CourtCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(14, 8, (card, _) => RankOf.Card(card) >= 11 ? 1 : 0, upgrade: 4);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Choose a card in your hand. It gets the rank of the last card on the Table.</summary>
public sealed class PairUp : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public PairUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(0, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var last = TableCmd.Cards(Owner.Creature).LastOrDefault(t => !t.Wild);
        if (last.Rank == 0) return;
        var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c is CardSharpCard && c != this, this);
        foreach (var c in chosen.OfType<CardSharpCard>()) c.SetRank(last.Rank);
        if (DynamicVars.Cards.BaseValue > 0) await DrawCards(ctx);
    }
}

/// <summary>Choose a card in your hand. Its rank becomes one higher than the last card on the Table. Draw 1 card.</summary>
public sealed class NextInLine : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public NextInLine() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var last = TableCmd.Cards(Owner.Creature).LastOrDefault(t => !t.Wild);
        if (last.Rank > 0)
        {
            var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c is CardSharpCard && c != this, this);
            var next = last.Rank >= Poker.MaxRank ? Poker.MinRank : last.Rank + 1;
            foreach (var c in chosen.OfType<CardSharpCard>()) c.SetRank(next);
        }
        await DrawCards(ctx);
    }
}

/// <summary>The next 3 cards you play count as the suit of the last card on the Table.</summary>
public sealed class FlushOut : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public FlushOut() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Count", 3, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var table = await TableCmd.Get(ctx, Owner.Creature);
        var last = table.Cards.LastOrDefault(t => !t.Wild);
        table.SuitOverride = table.Cards.Count == 0 ? Suit.Clubs : last.Suit;
        table.SuitOverrideCount = V("Count") + 1; // +1: this card is laid right after and uses one
    }
}

/// <summary>Showdown now. If it was a Pair or better, gain 2 Energy.</summary>
public sealed class DoubleOrNothing : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    protected override int LayCount => 0;
    public DoubleOrNothing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(2); WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await TableCmd.Lay(ctx, this);
        var hand = TableCmd.Count(Owner.Creature) > 0 ? await TableCmd.Showdown(ctx, this) : null;
        if (hand is >= HandType.Pair) await Energy(DynamicVars.Energy.BaseValue);
    }
}

/// <summary>Clear the Table. Draw 1 card for each card cleared.</summary>
public sealed class CashOut : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public CashOut() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { WithCostUpgradeBy(-1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var n = TableCmd.Clear(Owner.Creature);
        if (n > 0) await CardPileCmd.Draw(ctx, n, Owner);
    }
}

/// <summary>Gain 12 Block. Next turn, gain 6 Block.</summary>
public sealed class Insurance : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public Insurance() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(12, 3); WithPower<BlockNextTurnPower>(6, 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await Buff<BlockNextTurnPower>(ctx); }
}

/// <summary>Gain 5 Block. Retain.</summary>
public sealed class PokerChips : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public PokerChips() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(6, 3); WithKeywords(CardKeyword.Retain);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Deal 4 damage to ALL enemies twice.</summary>
public sealed class Showboat : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Showboat() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(4, 2); WithVar("Repeat", 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

/// <summary>Apply 1 Vulnerable to ALL enemies. Draw 1 card.</summary>
public sealed class MarkedCards : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public MarkedCards() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<VulnerablePower>(1, 1); WithCards(1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await DebuffAll<VulnerablePower>(ctx); await DrawCards(ctx); }
}

/// <summary>Draw 3 cards. Discard 1 card.</summary>
public sealed class ShuffleTracking : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public ShuffleTracking() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { WithCards(3, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await DrawCards(ctx);
        var discard = await CardSelectCmd.FromHandForDiscard(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c != this, this);
        await CardCmd.Discard(ctx, discard);
    }
}

/// <summary>Deal 8 damage. Draw 1 card.</summary>
public sealed class Hustle : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Hustle() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { WithDamage(8, 3); WithCards(1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await DrawCards(ctx); }
}

/// <summary>Apply 3 Weak.</summary>
public sealed class BluffCall : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public BluffCall() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { WithPower<WeakPower>(3, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Debuff<WeakPower>(ctx, play);
}

/// <summary>Deal 3 damage to a random enemy 8 times.</summary>
public sealed class FiftyTwoPickup : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public FiftyTwoPickup() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
        WithDamage(3, 1); WithVar("Repeat", 8);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

/// <summary>Draw 1 card. It becomes an Ace.</summary>
public sealed class AceUpTheSleeve : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public AceUpTheSleeve() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { WithCards(1, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var drawn = await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        foreach (var c in drawn.OfType<CardSharpCard>()) c.SetRank(Poker.MaxRank);
    }
}

/// <summary>Deal 13 damage. Lose 2 HP.</summary>
public sealed class SuckerBet : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public SuckerBet() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { WithDamage(15, 5); WithVar("HpLoss", 2); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await LoseHp(ctx, V("HpLoss")); }
}

/// <summary>Gain Block equal to this card's rank, plus 2.</summary>
public sealed class RoyalGuard : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public RoyalGuard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(2, 1, (card, _) => RankOf.Card(card), upgrade: 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Co-op only. Another player draws 2 cards.</summary>
public sealed class Kitty : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public Kitty() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { WithCards(2, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target?.Player != null) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, play.Target.Player);
    }
}

/// <summary>Co-op only. ALL players gain 4 Block.</summary>
public sealed class ChipIn : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public ChipIn() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { WithBlock(5, 2); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var ally in Allies) await CreatureCmd.GainBlock(ally, DynamicVars.Block, play);
    }
}

// =====================================================================
//  RARE — powers
// =====================================================================

public sealed class CardCounter() : SharpPowerCard<CardCounterPower>(Suit.Diamonds, 2, CardRarity.Rare, 1, 0, -1);
public sealed class HouseEdge() : SharpPowerCard<HouseEdgePower>(Suit.Spades, 3, CardRarity.Rare, 1, 0, -1);
public sealed class LadyLuck() : SharpPowerCard<LadyLuckPower>(Suit.Hearts, 2, CardRarity.Rare, 1, 0, -1);
public sealed class HighRoller() : SharpPowerCard<HighStakesPower>(Suit.Spades, 3, CardRarity.Rare, 100, 0, -1);
public sealed class AceCollector() : SharpPowerCard<AceCollectorPower>(Suit.Diamonds, 1, CardRarity.Rare, 2, 1);
public sealed class Bankroll() : SharpPowerCard<BankrollPower>(Suit.Hearts, 1, CardRarity.Rare, 2, 1);
public sealed class Showman() : SharpPowerCard<ShowmanPower>(Suit.Spades, 2, CardRarity.Rare, 5, 2);
public sealed class DiamondMine() : SharpPowerCard<DiamondMinePower>(Suit.Diamonds, 1, CardRarity.Rare, 3, 1);
public sealed class JokersWild : SharpPowerCard<JokersWildPower>
{
    public JokersWild() : base(Suit.Diamonds, 2, CardRarity.Rare, 1, 0, -1) => WithTip(typeof(Joker));
}

// =====================================================================
//  RARE — skills and attacks
// =====================================================================

/// <summary>Deal damage equal to the total rank of the cards on the Table to ALL enemies.</summary>
public sealed class RoyalFlush : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public RoyalFlush() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCalculatedDamage(0, 1, (card, _) => Poker.TotalRank(TableCmd.Cards(card.Owner.Creature)), upgrade: 8);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Showdown now. It counts as two hands higher. Exhaust.</summary>
public sealed class Jackpot : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    protected override int LayCount => 0;
    public Jackpot() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Tiers", 2); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await TableCmd.Lay(ctx, this);
        if (TableCmd.Count(Owner.Creature) > 0) await TableCmd.Showdown(ctx, this, V("Tiers"));
    }
}

/// <summary>Deal 8 damage. If the Table has Two Pair or better, hit 3 times.</summary>
public sealed class DeadMansHand : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public DeadMansHand() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(8, 3); WithVar("Repeat", 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await Hit(ctx, play, TableCmd.HasAtLeast(Owner.Creature, HandType.TwoPair) ? V("Repeat") : 1);
}

/// <summary>All cards in your hand get new ranks. Draw 2 cards.</summary>
public sealed class Reshuffle : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public Reshuffle() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { WithCards(2, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var c in HandCards.Where(c => !c.IsWild)) c.RollRank();
        await DrawCards(ctx);
    }
}

/// <summary>All cards in your hand become Aces. Exhaust.</summary>
public sealed class MarkedAces : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public MarkedAces() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var c in HandCards) c.SetRank(Poker.MaxRank);
        SetRank(Poker.MaxRank);
        return Task.CompletedTask;
    }
}

/// <summary>Gain Block equal to the total rank of the cards on the Table.</summary>
public sealed class HighSociety : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public HighSociety() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCalculatedBlock(0, 1, (card, _) => Poker.TotalRank(TableCmd.Cards(card.Owner.Creature)), upgrade: 8);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Showdown now, then Showdown again with the same cards. Exhaust.</summary>
public sealed class FinalHand : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    protected override int LayCount => 0;
    public FinalHand() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await TableCmd.Lay(ctx, this);
        var cards = TableCmd.Cards(Owner.Creature).ToList();
        if (cards.Count == 0) return;
        await TableCmd.Showdown(ctx, this);
        await TableCmd.Restore(ctx, Owner.Creature, cards);
        await TableCmd.Showdown(ctx, this);
    }
}

/// <summary>Deal 30 damage. Clear the Table.</summary>
public sealed class AllOrNothing : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    protected override int LayCount => 0;
    public AllOrNothing() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { WithDamage(30, 10); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        TableCmd.Clear(Owner.Creature);
    }
}

/// <summary>Deal 7 damage. If the ranks on the Table add up to 21 or more, deal 21 instead.</summary>
public sealed class Blackjack : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public Blackjack() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, 14, (card, _) => Poker.TotalRank(TableCmd.Cards(card.Owner.Creature)) >= 21 ? 1 : 0, upgrade: 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Gain 1 Intangible. Exhaust.</summary>
public sealed class FakeOut : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public FakeOut() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<IntangiblePower>(1); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Buff<IntangiblePower>(ctx);
}

/// <summary>Apply 2 Weak and 2 Vulnerable to ALL enemies. Exhaust.</summary>
public sealed class BigBlind : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public BigBlind() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(2, 1); WithPower<VulnerablePower>(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await DebuffAll<WeakPower>(ctx); await DebuffAll<VulnerablePower>(ctx); }
}

/// <summary>Gain 2 Energy. Draw 2 cards. Exhaust.</summary>
public sealed class HotStreak : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public HotStreak() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(2); WithCards(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Energy(DynamicVars.Energy.BaseValue); await DrawCards(ctx); }
}

/// <summary>Deal 10 damage. If your hand is empty, deal double.</summary>
public sealed class LastCard : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public LastCard() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 10, (card, _) => (card.Owner.PlayerCombatState?.Hand.Cards.Count(c => c != card) ?? 1) == 0 ? 1 : 0, upgrade: 4, bonusUpgrade: 4);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Deal 10 damage. All cards in your hand go up one rank.</summary>
public sealed class RaiseTheStakes : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public RaiseTheStakes() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { WithDamage(10, 4); WithVar("Ranks", 1, 1); }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        foreach (var c in HandCards.Where(c => !c.IsWild)) c.SetRank(c.EnsureRank() + V("Ranks"));
    }
}

/// <summary>Co-op only. ALL players gain 1 Energy. Exhaust.</summary>
public sealed class SharedFortune : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public SharedFortune() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithEnergy(1); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var ally in Allies)
            if (ally.Player != null) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, ally.Player);
    }
}

/// <summary>Apply 1 Weak. Draw 1 card.</summary>
public sealed class Tell : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public Tell() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 1); WithCards(1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) { await Debuff<WeakPower>(ctx, play); await DrawCards(ctx); }
}
