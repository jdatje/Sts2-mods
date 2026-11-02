using BaseLib.Utils;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Golem.GolemCode.Cards;

/// <summary>Deal 7 damage to ALL enemies.</summary>
public sealed class Rockslide : GolemCard
{
    public Rockslide() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(7, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
    }
}

/// <summary>Deal 18 damage.</summary>
public sealed class BoulderToss : GolemCard
{
    public BoulderToss() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(18, 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
    }
}

/// <summary>Deal 6 damage. Apply 1 Weak.</summary>
public sealed class Mudslinger : GolemCard
{
    public Mudslinger() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithPower<WeakPower>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
        if (cardPlay.Target != null)
            await CommonActions.Apply<WeakPower>(ctx, cardPlay.Target, this);
    }
}

/// <summary>Deal 4 damage to a random enemy 3 times.</summary>
public sealed class PebbleBarrage : GolemCard
{
    public PebbleBarrage() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        WithDamage(4, 1);
        WithVar("Repeat", 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, DynamicVars["Repeat"].IntValue).Execute(ctx);
    }
}

/// <summary>Gain 4 Plating.</summary>
public sealed class StoneSkin : GolemCard
{
    public StoneSkin() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<PlatingPower>(4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<PlatingPower>(ctx, this);
    }
}

/// <summary>Gain 15 Block.</summary>
public sealed class PackedEarth : GolemCard
{
    public PackedEarth() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(15, 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }
}

/// <summary>Gain 6 Block, plus 3 for each Part you have.</summary>
public sealed class Assemble : GolemCard
{
    public Assemble() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(6, 3, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 2, bonusUpgrade: 1);
        WithTip(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }
}

/// <summary>Deal 10 damage, plus 5 for each Part you have.</summary>
public sealed class FullBodySlam : GolemCard
{
    public FullBodySlam() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 5, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 3, bonusUpgrade: 2);
        WithTip(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
    }
}
