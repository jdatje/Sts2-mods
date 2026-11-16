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

/// <summary>House Rules that change how hands are scored.</summary>
/// <param name="Suited">A Flush needs only 4 cards of one suit (out of 5).</param>
/// <param name="GapFiller">A Straight may have one gap.</param>
/// <param name="AceHigh">A High card hand with an Ace counts as a Pair.</param>
public readonly record struct PokerRules(bool Suited = false, bool GapFiller = false, bool AceHigh = false);

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
    /// <param name="size">Showdown size (5, or 4 with Card Shark): Straights and Flushes need exactly this many cards.</param>
    public static HandType Evaluate(IReadOnlyList<TableCard> cards, int size = 5, PokerRules rules = default)
    {
        if (cards.Count == 0) return HandType.HighCard;
        var hand = cards.Any(c => c.Wild) ? EvaluateWithWilds(cards, size, rules) : EvaluateFixed(cards, size, rules);
        if (rules.AceHigh && hand == HandType.HighCard && cards.Any(c => !c.Wild && c.Rank == MaxRank))
            hand = HandType.Pair;
        return hand;
    }

    /// <summary>Tries every rank/suit for each wild card (1 to 3 wilds).</summary>
    private static HandType EvaluateWithWilds(IReadOnlyList<TableCard> cards, int size, PokerRules rules)
    {
        var fixedCards = cards.Where(c => !c.Wild).ToList();
        var wilds = cards.Count(c => c.Wild);
        // 4+ wilds: with 5 cards a straight flush is always possible around the one fixed card,
        // with 4 cards the best is four of a kind.
        if (wilds >= 4) return cards.Count >= 5 || (size == 4 && cards.Count == 4) ? HandType.StraightFlush : HandType.FourOfAKind;
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
                var h = EvaluateFixed(hand, size, rules);
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

    private static HandType EvaluateFixed(IReadOnlyList<TableCard> cards, int size = 5, PokerRules rules = default)
    {
        if (cards.Count == 0) return HandType.HighCard;

        var counts = cards.GroupBy(c => c.Rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
        var full = cards.Count == 5 || (size == 4 && cards.Count == 4);
        var maxSuit = cards.GroupBy(c => c.Suit).Max(g => g.Count());
        var flush = full && (maxSuit == cards.Count || (rules.Suited && cards.Count == 5 && maxSuit >= 4));
        var straight = full && IsStraight(cards.Select(c => c.Rank).ToList(), rules.GapFiller);

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

    private static bool IsStraight(List<int> ranks, bool allowGap = false)
    {
        var distinct = ranks.Distinct().OrderBy(r => r).ToList();
        var n = ranks.Count;
        if (distinct.Count != n || n < 4) return false;
        var span = allowGap ? n : n - 1;
        if (distinct[n - 1] - distinct[0] <= span) return true;
        // Ace low (the wheel: A-2-3-4(-5))
        if (distinct[n - 1] == MaxRank)
        {
            var low = distinct.Select(r => r == MaxRank ? 1 : r).OrderBy(r => r).ToList();
            if (low[n - 1] - low[0] <= span) return true;
        }
        return false;
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
