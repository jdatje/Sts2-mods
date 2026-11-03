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

/// <summary>
/// One card laid on the Table: rank 2..14 (11 J, 12 Q, 13 K, 14 A) and suit.
/// A Wild card (Joker) counts as whatever rank and suit makes the best hand.
/// </summary>
public readonly record struct TableCard(int Rank, Suit Suit, bool Wild = false);

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
        if (cards.Any(c => c.Wild)) return EvaluateWithWilds(cards);
        return EvaluateFixed(cards);
    }

    /// <summary>Tries every rank/suit for each wild card (1 to 3 wilds).</summary>
    private static HandType EvaluateWithWilds(IReadOnlyList<TableCard> cards)
    {
        var fixedCards = cards.Where(c => !c.Wild).ToList();
        var wilds = cards.Count(c => c.Wild);
        // 4+ wilds: with 5 cards a straight flush is always possible around the one fixed card,
        // with 4 cards the best is four of a kind.
        if (wilds >= 4) return cards.Count >= 5 ? HandType.StraightFlush : HandType.FourOfAKind;
        var expand = wilds;

        var options = new List<TableCard>();
        for (var r = MinRank; r <= MaxRank; r++)
            foreach (var s in Enum.GetValues<Suit>())
                options.Add(new TableCard(r, s));

        var best = HandType.HighCard;
        void Search(List<TableCard> hand, int left)
        {
            if (best == HandType.StraightFlush) return;
            if (left == 0)
            {
                var h = EvaluateFixed(hand);
                if (h > best) best = h;
                return;
            }
            foreach (var o in options)
            {
                hand.Add(o);
                Search(hand, left - 1);
                hand.RemoveAt(hand.Count - 1);
            }
        }
        Search(fixedCards, expand);
        return best;
    }

    private static HandType EvaluateFixed(IReadOnlyList<TableCard> cards)
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

    public static HandType TiersHigher(HandType t, int tiers) =>
        (HandType)Math.Clamp((int)t + tiers, 0, (int)HandType.StraightFlush);

    /// <summary>Sum of ranks on the Table (wild cards count as 0).</summary>
    public static int TotalRank(IReadOnlyList<TableCard> cards) => cards.Where(c => !c.Wild).Sum(c => c.Rank);

    /// <summary>The suit most represented on the Table (used for the Flush bonus).</summary>
    public static Suit MainSuit(IReadOnlyList<TableCard> cards) =>
        cards.Where(c => !c.Wild).GroupBy(c => c.Suit).OrderByDescending(g => g.Count()).Select(g => g.Key)
            .DefaultIfEmpty(Suit.Spades).First();
}
