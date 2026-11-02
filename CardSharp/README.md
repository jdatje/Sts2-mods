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

## Cards (v0.1)

| Card | Suit | Rarity | Cost | Text |
|---|---|---|---|---|
| Strike | ♠ | Basic | 1 | Deal 6 damage. |
| Defend | ♥ | Basic | 1 | Gain 5 Block. |
| Ante | ♦ | Basic | 1 | Draw 2 cards. |
| Cut the Deck | ♣ | Basic | 1 | Apply 2 Vulnerable. |
| Quick Draw | ♠ | Common | 0 | Deal 4 damage. |
| Double Tap | ♠ | Common | 1 | Deal 5 damage twice. |
| Bluff | ♥ | Common | 1 | Gain 8 Block. |
| Poker Face | ♥ | Common | 2 | Gain 15 Block. |
| Shuffle Up | ♦ | Common | 1 | Gain 3 Block. Draw 2. |
| Deal In | ♦ | Common | 0 | Draw 1. |
| Card Trick | ♣ | Common | 1 | Apply 1 Weak and 1 Vulnerable. |
| Sleight of Hand | ♦ | Common | 1 (0) | A card in your hand becomes an Ace. |
| Double Down | ♠ | Uncommon | 1 | Deal 7 damage. Laid on the Table twice. |
| Fold | ♥ | Uncommon | 0 | Clear the Table. 4 Block per card cleared. |
| Call | ♣ | Uncommon | 1 | Apply 1 Weak. Showdown now. |
| Cold Deck | ♣ | Uncommon | 1 (0) | Choose a card; your whole hand gets its rank. |
| Stack the Deck | ♦ | Uncommon | 1 | The next 2 cards count as Diamonds. |
| Shared Pot | ♦ | Uncommon | 1 | Co-op only. ALL players draw 1. |
| All In | ♠ | Rare | 2 | 6 damage to ALL enemies per card on the Table. |
| Card Shark | ♦ | Rare | 2 (1) | Power. Showdowns happen at 4 cards. |

The hand evaluator is unit-tested in `tests/PokerTests`.
