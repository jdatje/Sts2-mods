using CardSharp.CardSharpCode.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CardSharp.CardSharpCode.Table;

/// <summary>House Rules: picked at the rest site ("Deal Me In"), kept for the whole run.</summary>
public enum HouseRule
{
    MatchedSet,  // Pair and Two pair hit ALL enemies
    FullTable,   // +3 damage per card sharing its rank with another card
    RoyalCourt,  // +2 damage per J/Q/K/A
    AceHigh,     // High card with an Ace counts as a Pair
    Suited,      // Flush needs only 4 of one suit
    ColourUp,    // 3+ of one suit give half that suit's Flush bonus
    GapFiller,   // Straights may have one gap
    LongRun,     // Straights deal double damage and draw 1 more
    QuickDeal,   // first Showdown each combat at 3 cards
    ShortStack,  // Showdowns with 4 or fewer cards deal +5 damage
}

/// <summary>
/// Hand Levels: every poker hand levels up over the run by being scored. Rarer hands level faster.
/// Per Level: +25% of the hand's base damage (at least +2); Two pair and Full house also +25% Block.
/// </summary>
public static class HandLevels
{
    public const int MaxLevel = 10;

    /// <summary>Scores needed per Level (0 = never levels by playing).</summary>
    public static int ScoresPerLevel(HandType hand) => hand switch
    {
        HandType.HighCard => 0,
        HandType.Pair => 6,
        HandType.TwoPair => 3,
        HandType.ThreeOfAKind => 2,
        _ => 1,
    };

    /// <summary>Base damage and Block of each hand (before Levels, Pot, Strength and bonuses).</summary>
    public static (int Damage, int Block, int Draw, int Energy) Base(HandType hand) => hand switch
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

    public static int DamagePerLevel(HandType hand) =>
        Math.Max(2, (int)Math.Round(Base(hand).Damage * 0.25, MidpointRounding.AwayFromZero));

    public static int BlockPerLevel(HandType hand) =>
        (int)Math.Round(Base(hand).Block * 0.25, MidpointRounding.AwayFromZero);

    public static int LevelFor(HandType hand, int scores)
    {
        var per = ScoresPerLevel(hand);
        return per <= 0 ? 0 : Math.Min(MaxLevel, scores / per);
    }

    public static PotRelic? Relic(Player? player) => player?.Relics.OfType<PotRelic>().FirstOrDefault();
    public static PotRelic? Relic(Creature? creature) => Relic(creature?.Player);

    public static int Level(Creature? owner, HandType hand) => Relic(owner)?.HandLevel(hand) ?? 0;

    public static bool Has(Creature? owner, HouseRule rule) => Relic(owner)?.HasRule(rule) ?? false;

    public static PokerRules Rules(Creature? owner) => new(
        Suited: Has(owner, HouseRule.Suited),
        GapFiller: Has(owner, HouseRule.GapFiller),
        AceHigh: Has(owner, HouseRule.AceHigh));
}
