# The Card Sharp

A gambler whose cards have **suits and ranks**.

- **Suit** is fixed per card: ♠ Spades = attacks, ♥ Hearts = block, ♦ Diamonds = draw/energy, ♣ Clubs = debuffs.
- **Rank** (2 to Ace) is rolled every time the card enters your hand, using the run's combat RNG
  (the same on every machine in co-op). The top line of each card shows it, e.g. "Queen of Spades".
- Every card you play is laid on the **Table** (a power; its number is the cards on the Table).
- At 5 cards: **Showdown**. The Table is scored as a poker hand, the effect happens, the Table clears.

| Hand | Effect (ALL enemies) |
|---|---|
| High card | 5 damage |
| Pair | 9 damage |
| Two pair | 9 damage, 8 Block |
| Three of a kind | 16 damage |
| Straight | 12 damage, draw 2 |
| Flush | 14 damage + suit bonus (♠ +6 damage, ♥ +10 Block, ♦ draw 2, ♣ 2 Weak to ALL) |
| Full house | 22 damage, 12 Block |
| Four of a kind | 32 damage |
| Straight flush | 45 damage, 2 Energy |

Starter: 4 Strike ♠, 4 Defend ♥, Ante ♦ (draw 2), Cut the Deck ♣ (2 Vulnerable). Relic **Marked Deck**:
your first Showdown each combat counts as one hand higher. HP 68.

## Version 0.2: full card pool, 86 cards

4 basic, 20 common, 36 uncommon, 26 rare, plus the **Joker** token (wild: counts as any rank and suit).

- **Showdown engines:** powers that trigger on every Showdown (Lucky Streak, Croupier, Poker Night, Card Counter,
  Bankroll, Showman, Jokers Wild) or make it stronger (High Stakes, High Roller, House Edge, Card Shark).
- **Rank play:** cards that use their own rank (Hole Card, Lowball, Royal Guard, Court Card), change ranks
  (Sleight of Hand, Cold Deck, Pair Up, Next in Line, Marked Aces, Reshuffle, Raise the Stakes, Lady Luck,
  Loaded Deck) or read the whole Table (Royal Flush, High Society, Blackjack, Re-raise).
- **Suits:** Stack the Deck and Flush Out force flushes; Pit Boss rewards Spades, Diamond Mine rewards Diamonds.
- **Timing:** Call, Double or Nothing, Jackpot and Final Hand Showdown on demand; Fold and Cash Out clear the Table.

**Full card list:** [docs/cards-cardsharp.md](../docs/cards-cardsharp.md). The hand evaluator (including Jokers)
is unit-tested in `tests/PokerTests`.
