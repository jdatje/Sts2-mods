using BaseLib.Extensions;
using BaseLib.Utils;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Golem.GolemCode.Cards;

// ---------- Uncommon Parts ----------

/// <summary>Part (Arms). Whenever you play an Attack, gain 3 Block.</summary>
public sealed class BoulderFists : GolemCard
{
    public BoulderFists() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<BoulderFistsPower>(3, 1);
        WithKeywords(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PartCmd.Install<BoulderFistsPower>(ctx, this, DynamicVars.Power<BoulderFistsPower>().BaseValue);
}

/// <summary>Part (Head). Draw 1 additional card each turn. Upgrade: costs 0.</summary>
public sealed class LanternEye : GolemCard
{
    public LanternEye() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<LanternEyePower>(1);
        WithKeywords(GolemKeywords.Part);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PartCmd.Install<LanternEyePower>(ctx, this, DynamicVars.Power<LanternEyePower>().BaseValue);
}

/// <summary>Part (Legs). At the end of your turn, gain 4 Block.</summary>
public sealed class RootedLegs : GolemCard
{
    public RootedLegs() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<RootedLegsPower>(3, 1);
        WithKeywords(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PartCmd.Install<RootedLegsPower>(ctx, this, DynamicVars.Power<RootedLegsPower>().BaseValue);
}

/// <summary>Part (Core). Block from cards is increased by 2.</summary>
public sealed class MudCore : GolemCard
{
    public MudCore() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<MudCorePower>(2, 1);
        WithKeywords(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PartCmd.Install<MudCorePower>(ctx, this, DynamicVars.Power<MudCorePower>().BaseValue);
}

// ---------- Uncommon others ----------

/// <summary>Remove all your Parts. Gain 7 Block for each Part removed. Exhaust.</summary>
public sealed class Collapse : GolemCard
{
    public Collapse() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("PerPart", 7, 3);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(GolemKeywords.Part);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var removed = await PartCmd.RemoveAll(ctx, Owner.Creature);
        if (removed > 0)
            await CreatureCmd.GainBlock(Owner.Creature, removed * DynamicVars["PerPart"].BaseValue, ValueProp.Move, cardPlay);
    }
}

/// <summary>Apply 1 Weak and 1 Vulnerable to ALL enemies.</summary>
public sealed class Tremor : GolemCard
{
    public Tremor() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1);
        WithPower<VulnerablePower>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<WeakPower>(ctx, enemies, this);
        await CommonActions.Apply<VulnerablePower>(ctx, enemies, this);
    }
}

/// <summary>Co-op only. Give another player 10 Block.</summary>
public sealed class LendAHand : GolemCard
{
    public LendAHand() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithBlock(10, 3);
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay);
    }
}

// ---------- Rare ----------

/// <summary>Part (Core). At the start of your turn, gain 1 Energy. Upgrade: costs 2.</summary>
public sealed class FurnaceCore : GolemCard
{
    public FurnaceCore() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<FurnaceCorePower>(1);
        WithEnergy(1); // card text only: energyIcons() needs an energy var
        WithKeywords(GolemKeywords.Part);
        WithCostUpgradeBy(-1);
        WithEnergyTip();
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PartCmd.Install<FurnaceCorePower>(ctx, this, DynamicVars.Power<FurnaceCorePower>().BaseValue);
}

/// <summary>Deal 8 damage to ALL enemies 3 times.</summary>
public sealed class Avalanche : GolemCard
{
    public Avalanche() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(8, 2);
        WithVar("Repeat", 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay, DynamicVars["Repeat"].IntValue).Execute(ctx);
    }
}
