using BaseLib.Extensions;
using BaseLib.Utils;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Golem.GolemCode.Cards;

public sealed class StrikeGolem : GolemCard
{
    public StrikeGolem() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithTags(CardTag.Strike);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
    }
}

public sealed class DefendGolem : GolemCard
{
    public DefendGolem() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTags(CardTag.Defend);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }
}

/// <summary>Deal 10 damage. Gain 5 Block.</summary>
public sealed class Slam : GolemCard
{
    public Slam() : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
        WithBlock(5, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(ctx);
        await CommonActions.CardBlock(this, cardPlay);
    }
}

/// <summary>Part (Arms). Your Attacks deal 2 additional damage.</summary>
public sealed class ClayArms : GolemCard
{
    public ClayArms() : base(1, CardType.Power, CardRarity.Basic, TargetType.Self)
    {
        WithPower<ClayArmsPower>(2, 1);
        WithKeywords(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PartCmd.Install<ClayArmsPower>(ctx, this, DynamicVars.Power<ClayArmsPower>().BaseValue);
    }
}
