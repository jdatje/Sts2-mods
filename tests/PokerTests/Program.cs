using CardSharp.CardSharpCode.Table;
TableCard C(int r, Suit s) => new(r, s);
TableCard W() => new(0, Suit.Spades, true);
var S=Suit.Spades; var H=Suit.Hearts; var D=Suit.Diamonds; var K=Suit.Clubs;
var cases = new (string name, TableCard[] cards, HandType want)[] {
 ("high", [C(2,S),C(5,H),C(9,D),C(11,K),C(13,S)], HandType.HighCard),
 ("pair", [C(7,S),C(7,H),C(9,D),C(11,K),C(13,S)], HandType.Pair),
 ("two pair", [C(7,S),C(7,H),C(9,D),C(9,K),C(13,S)], HandType.TwoPair),
 ("trips", [C(7,S),C(7,H),C(7,D),C(9,K),C(13,S)], HandType.ThreeOfAKind),
 ("straight", [C(5,S),C(6,H),C(7,D),C(8,K),C(9,S)], HandType.Straight),
 ("wheel", [C(14,S),C(2,H),C(3,D),C(4,K),C(5,S)], HandType.Straight),
 ("broadway", [C(10,S),C(11,H),C(12,D),C(13,K),C(14,S)], HandType.Straight),
 ("not straight (wrap)", [C(12,S),C(13,H),C(14,D),C(2,K),C(3,S)], HandType.HighCard),
 ("flush", [C(2,H),C(5,H),C(9,H),C(11,H),C(13,H)], HandType.Flush),
 ("full house", [C(7,S),C(7,H),C(7,D),C(9,K),C(9,S)], HandType.FullHouse),
 ("quads", [C(7,S),C(7,H),C(7,D),C(7,K),C(9,S)], HandType.FourOfAKind),
 ("straight flush", [C(5,K),C(6,K),C(7,K),C(8,K),C(9,K)], HandType.StraightFlush),
 ("4 cards quads", [C(7,S),C(7,H),C(7,D),C(7,K)], HandType.FourOfAKind),
 ("4 cards two pair", [C(7,S),C(7,H),C(9,D),C(9,K)], HandType.TwoPair),
 ("4 same suit no flush", [C(2,H),C(5,H),C(9,H),C(11,H)], HandType.HighCard),
 ("1 card", [C(14,S)], HandType.HighCard),
 ("wild makes pair", [C(7,S),C(2,H),C(9,D),C(11,K),W()], HandType.Pair),
 ("wild makes straight", [C(5,S),C(6,H),C(8,D),C(9,K),W()], HandType.Straight),
 ("wild makes flush", [C(2,H),C(5,H),C(9,H),C(11,H),W()], HandType.Flush),
 ("wild + trips = quads", [C(7,S),C(7,H),C(7,D),C(2,K),W()], HandType.FourOfAKind),
 ("wild straight flush", [C(5,K),C(6,K),C(7,K),C(9,K),W()], HandType.StraightFlush),
 ("two wilds + pair = quads", [C(7,S),C(7,H),C(2,D),W(),W()], HandType.FourOfAKind),
 ("only wilds", [W(),W(),W(),W(),W()], HandType.StraightFlush),
 ("4 cards with wild", [C(7,S),C(7,H),C(7,D),W()], HandType.FourOfAKind),
};
int fail=0;
var size4 = new (string name, TableCard[] cards, HandType want)[] {
 ("4-card flush (Card Shark)", [C(2,H),C(5,H),C(9,H),C(11,H)], HandType.Flush),
 ("4-card straight (Card Shark)", [C(11,S),C(12,H),C(13,D),C(14,K)], HandType.Straight),
 ("4-card wheel (Card Shark)", [C(14,S),C(2,H),C(3,D),C(4,K)], HandType.Straight),
 ("4-card straight flush", [C(5,K),C(6,K),C(7,K),C(8,K)], HandType.StraightFlush),
 ("3 cards same suit, size 4", [C(2,H),C(5,H),C(9,H)], HandType.HighCard),
};
foreach (var (n,c,w) in size4){ var got=Poker.Evaluate(c,4); if(got!=w){fail++; Console.WriteLine($"FAIL {n}: got {got}, want {w}");} }
foreach (var (n,c,w) in cases){ var got=Poker.Evaluate(c); if(got!=w){fail++; Console.WriteLine($"FAIL {n}: got {got}, want {w}");} }
Console.WriteLine($"TotalRank={Poker.TotalRank([C(7,S),C(14,H),W()])} (want 21), TiersHigher(Pair,2)={Poker.TiersHigher(HandType.Pair,2)}");
Console.WriteLine($"Poker tests: {cases.Length-fail}/{cases.Length} passed; OneTierHigher(Pair)={Poker.OneTierHigher(HandType.Pair)}, StraightFlush stays {Poker.OneTierHigher(HandType.StraightFlush)}");
return fail == 0 ? 0 : 1;
