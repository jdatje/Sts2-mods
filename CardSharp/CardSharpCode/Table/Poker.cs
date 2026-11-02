namespace CardSharp.CardSharpCode.Table;

/// <summary>Every Card Sharp card has a fixed suit, tied to what it does.</summary>
public enum Suit
{
    Spades,   // attacks
    Hearts,   // block
    Diamonds, // draw / energy
    Clubs     // debuffs
}

public enum HandType
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush
}

/// <summary>One card laid on the Table: rank 2..14 (11 J, 12 Q, 13 K, 14 A) and suit.</summary>
public readonly record struct TableCard(int Rank, Suit Suit);

public static class Poker
{
    public const int MinRank = 2;
    public const int MaxRank = 14;

    public static string RankName(int rank) => rank switch
    {
        11 => "Jack",
        12 => "Queen",
        13 => "King",
        14 => "Ace",
        _ => rank.ToString()
    };

    /// <summary>
    /// Scores 1 to 5 cards. Straights and flushes need all 5 cards; pairs, trips and quads
    /// work with fewer (for Call and Card Shark).
    /// </summary>
    public static HandType Evaluate(IReadOnlyList<TableCard> cards)
    {
        if (cards.Count == 0) return HandType.HighCard;

        var counts = cards.GroupBy(c => c.Rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
        var flush = cards.Count == 5 && cards.All(c => c.Suit == cards[0].Suit);
        var straight = cards.Count == 5 && IsStraight(cards.Select(c => c.Rank).ToList());

        if (straight && flush) return HandType.StraightFlush;
        if (counts[0] == 4) return HandType.FourOfAKind;
        if (counts[0] == 3 && counts.Count > 1 && counts[1] >= 2) return HandType.FullHouse;
        if (flush) return HandType.Flush;
        if (straight) return HandType.Straight;
        if (counts[0] == 3) return HandType.ThreeOfAKind;
        if (counts[0] == 2 && counts.Count > 1 && counts[1] == 2) return HandType.TwoPair;
        if (counts[0] == 2) return HandType.Pair;
        return HandType.HighCard;
    }

    private static bool IsStraight(List<int> ranks)
    {
        var distinct = ranks.Distinct().OrderBy(r => r).ToList();
        if (distinct.Count != 5) return false;
        if (distinct[4] - distinct[0] == 4) return true;
        // the wheel: A-2-3-4-5
        return distinct.SequenceEqual(new[] { 2, 3, 4, 5, 14 });
    }

    public static HandType OneTierHigher(HandType t) =>
        t == HandType.StraightFlush ? t : (HandType)((int)t + 1);

    /// <summary>The suit most represented on the Table (used for the Flush bonus).</summary>
    public static Suit MainSuit(IReadOnlyList<TableCard> cards) =>
        cards.GroupBy(c => c.Suit).OrderByDescending(g => g.Count()).First().Key;
}
