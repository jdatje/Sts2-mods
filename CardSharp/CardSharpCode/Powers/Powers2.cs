using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardSharp.CardSharpCode.Powers;

public abstract class SimpleSharpPower : CardSharpPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected IReadOnlyList<Creature> Enemies => CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];
    protected IReadOnlyList<Creature> Allies => CombatState?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? (IReadOnlyList<Creature>)[];
}

// ---------------- Showdown listeners ----------------

/// <summary>Whenever you Showdown, draw X cards.</summary>
public sealed class LuckyStreakPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

/// <summary>Whenever you Showdown, gain X Block.</summary>
public sealed class CroupierPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Whenever you Showdown, apply X Weak and X Vulnerable to ALL enemies.</summary>
public sealed class PokerNightPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        if (Enemies.Count == 0) return;
        Flash();
        await PowerCmd.Apply<WeakPower>(ctx, Enemies, Amount, Owner, null);
        await PowerCmd.Apply<VulnerablePower>(ctx, Enemies, Amount, Owner, null);
    }
}

/// <summary>Whenever you Showdown, gain X Energy.</summary>
public sealed class CardCounterPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        if (Owner.Player == null) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}

/// <summary>Whenever you Showdown, gain X Plating.</summary>
public sealed class BankrollPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        Flash();
        await PowerCmd.Apply<PlatingPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>Whenever you Showdown, ALL players gain X Block.</summary>
public sealed class ShowmanPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        Flash();
        foreach (var ally in Allies)
            await CreatureCmd.GainBlock(ally, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Whenever you Showdown, add X Jokers into your hand.</summary>
public sealed class JokersWildPower : SimpleSharpPower, IShowdownListener
{
    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Joker>(Owner, PileType.Hand, (int)Amount, Owner.Player);
    }
}

// ---------------- Showdown modifiers ----------------

/// <summary>Showdowns deal X% more damage.</summary>
public sealed class HighStakesPower : SimpleSharpPower, IShowdownModifier
{
    public override PowerStackType StackType => PowerStackType.Single;
    public int DamageBonusPercent => (int)Amount;
    public int TierBonus => 0;
}

/// <summary>Showdowns deal X% more damage. Doesn't stack with High Stakes: the stronger one counts.</summary>
public sealed class HighRollerPower : SimpleSharpPower, IShowdownModifier
{
    public override PowerStackType StackType => PowerStackType.Single;
    public int DamageBonusPercent => (int)Amount;
    public int TierBonus => 0;
}

/// <summary>Your Showdowns count as X hands higher.</summary>
public sealed class HouseEdgePower : SimpleSharpPower, IShowdownModifier
{
    public int DamageBonusPercent => 0;
    public int TierBonus => (int)Amount;
}

// ---------------- Other powers ----------------

/// <summary>Cards you draw have rank 8 or higher. Checked in CardSharpCard.RollRank.</summary>
public sealed class LoadedDeckPower : SimpleSharpPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>At the start of your turn, gain 2 Block for each card on the Table (X = per card).</summary>
public sealed class BlufferPower : SimpleSharpPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player) return;
        var n = TableCmd.Count(Owner);
        if (n == 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount * n, ValueProp.Unpowered, null);
    }
}

/// <summary>At the start of your turn, if the Table is empty, draw X cards.</summary>
public sealed class SidePotPower : SimpleSharpPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player || TableCmd.Count(Owner) > 0) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, player);
    }
}

/// <summary>Whenever you play a Spade, deal X damage to ALL enemies.</summary>
public sealed class PitBossPower : SimpleSharpPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || play.Card is not CardSharpCard { Suit: Suit.Spades, IsWild: false }) return;
        if (Enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx, Enemies, Amount, ValueProp.Unpowered, Owner);
    }
}

/// <summary>Whenever you play a Diamond, gain X Block.</summary>
public sealed class DiamondMinePower : SimpleSharpPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || play.Card is not CardSharpCard { Suit: Suit.Diamonds, IsWild: false }) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Whenever you play an Ace, draw X cards.</summary>
public sealed class AceCollectorPower : SimpleSharpPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || play.Card is not CardSharpCard { Rank: Poker.MaxRank, IsWild: false }) return;
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

/// <summary>At the start of your turn, X random cards in your hand become Aces.</summary>
public sealed class LadyLuckPower : SimpleSharpPower
{
    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player) return Task.CompletedTask;
        var hand = player.PlayerCombatState?.Hand.Cards.OfType<CardSharpCard>().Where(c => !c.IsWild && c.Rank != Poker.MaxRank).ToList() ?? [];
        var rng = player.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < Amount && hand.Count > 0; i++)
        {
            var pick = rng.NextItem(hand);
            if (pick == null) break;
            pick.SetRank(Poker.MaxRank);
            hand.Remove(pick);
        }
        if (Amount > 0) Flash();
        return Task.CompletedTask;
    }
}
