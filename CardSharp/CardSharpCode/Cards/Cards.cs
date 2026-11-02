using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Powers;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardSharp.CardSharpCode.Cards;

// ================= Basic =================

public sealed class StrikeSharp : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public StrikeSharp() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithTags(CardTag.Strike);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

public sealed class DefendSharp : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public DefendSharp() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTags(CardTag.Defend);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardBlock(this, play);
}

/// <summary>Draw 2 cards.</summary>
public sealed class Ante : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public Ante() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithCards(2, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.Draw(this, ctx);
}

/// <summary>Apply 2 Vulnerable.</summary>
public sealed class CutTheDeck : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public CutTheDeck() : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithPower<VulnerablePower>(2, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null) await CommonActions.Apply<VulnerablePower>(ctx, play.Target, this);
    }
}

// ================= Common =================

/// <summary>Deal 4 damage.</summary>
public sealed class QuickDraw : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public QuickDraw() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

/// <summary>Deal 5 damage twice.</summary>
public sealed class DoubleTap : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public DoubleTap() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithVar("Repeat", 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play, DynamicVars["Repeat"].IntValue).Execute(ctx);
}

/// <summary>Gain 8 Block.</summary>
public sealed class Bluff : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public Bluff() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(8, 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardBlock(this, play);
}

/// <summary>Gain 15 Block.</summary>
public sealed class PokerFace : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public PokerFace() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(15, 5);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardBlock(this, play);
}

/// <summary>Gain 3 Block. Draw 2 cards.</summary>
public sealed class ShuffleUp : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public ShuffleUp() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(3, 2);
        WithCards(2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Draw(this, ctx);
    }
}

/// <summary>Draw 1 card.</summary>
public sealed class DealIn : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public DealIn() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.Draw(this, ctx);
}

/// <summary>Apply 1 Weak and 1 Vulnerable.</summary>
public sealed class CardTrick : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public CardTrick() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 1);
        WithPower<VulnerablePower>(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target == null) return;
        await CommonActions.Apply<WeakPower>(ctx, play.Target, this);
        await CommonActions.Apply<VulnerablePower>(ctx, play.Target, this);
    }
}

/// <summary>Choose a card in your hand. It becomes an Ace. Upgrade: costs 0.</summary>
public sealed class SleightOfHand : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public SleightOfHand() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1),
            c => c is CardSharpCard && c != this, this);
        foreach (var c in chosen.OfType<CardSharpCard>())
            c.SetRank(Poker.MaxRank);
    }
}

// ================= Uncommon =================

/// <summary>Deal 7 damage. This card is laid on the Table twice.</summary>
public sealed class DoubleDown : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    protected override int LayCount => 2;
    public DoubleDown() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

/// <summary>Clear the Table. Gain 4 Block for each card cleared.</summary>
public sealed class Fold : CardSharpCard
{
    public override Suit Suit => Suit.Hearts;
    public Fold() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("PerCard", 4, 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var n = TableCmd.Clear(Owner.Creature);
        if (n > 0)
            await CreatureCmd.GainBlock(Owner.Creature, n * DynamicVars["PerCard"].BaseValue, ValueProp.Move, play);
    }
}

/// <summary>Apply 1 Weak. Then Showdown with whatever is on the Table.</summary>
public sealed class Call : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    protected override int LayCount => 0; // lays itself before the Showdown
    public Call() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null) await CommonActions.Apply<WeakPower>(ctx, play.Target, this);
        await TableCmd.Lay(ctx, this);
        var table = Owner.Creature.Powers.OfType<TablePower>().FirstOrDefault();
        if (table != null && table.Cards.Count > 0)
            await TableCmd.Showdown(ctx, this);
    }
}

/// <summary>Choose a card in your hand. All other cards in your hand get its rank.</summary>
public sealed class ColdDeck : CardSharpCard
{
    public override Suit Suit => Suit.Clubs;
    public ColdDeck() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var chosen = (await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1),
            c => c is CardSharpCard && c != this, this)).OfType<CardSharpCard>().FirstOrDefault();
        if (chosen == null) return;
        var rank = chosen.EnsureRank();
        foreach (var c in Owner.PlayerCombatState!.Hand.Cards.OfType<CardSharpCard>())
            c.SetRank(rank);
        SetRank(rank); // this card is laid with the same rank too
    }
}

/// <summary>The next 2 cards you play count as Diamonds.</summary>
public sealed class StackTheDeck : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public StackTheDeck() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Count", 2, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        var table = await TableCmd.Get(ctx, Owner.Creature);
        table.SuitOverride = Suit.Diamonds;
        // +1 because this card is laid right after its effect and uses one up (it's a Diamond anyway).
        table.SuitOverrideCount = DynamicVars["Count"].IntValue + 1;
    }
}

/// <summary>Co-op only. ALL players draw 1 card.</summary>
public sealed class SharedPot : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public SharedPot() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithCards(1, 1);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var creature in CombatState.PlayerCreatures)
            if (creature.Player != null)
                await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, creature.Player);
    }
}

// ================= Rare =================

/// <summary>Deal 6 damage to ALL enemies for each card on the Table.</summary>
public sealed class AllIn : CardSharpCard
{
    public override Suit Suit => Suit.Spades;
    public AllIn() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCalculatedDamage(0, 6, (card, _) =>
            card.Owner.Creature.Powers.OfType<TablePower>().FirstOrDefault()?.Cards.Count ?? 0, bonusUpgrade: 2);
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

/// <summary>Power. Showdowns happen at 4 cards instead of 5. Upgrade: costs 1.</summary>
public sealed class CardShark : CardSharpCard
{
    public override Suit Suit => Suit.Diamonds;
    public CardShark() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTip(typeof(CardSharkPower));
    }
    protected override async Task Effect(PlayerChoiceContext ctx, CardPlay play) =>
        await PowerCmd.Apply<CardSharkPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
}
