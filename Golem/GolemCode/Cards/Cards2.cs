using BaseLib.Extensions;
using BaseLib.Utils;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Golem.GolemCode.Cards;

/// <summary>Base for Part cards: a Power card that installs Part T with an amount (and upgrade).</summary>
public abstract class PartCard<T> : GolemCard where T : PartPower
{
    protected PartCard(int cost, CardRarity rarity, int amount, int upgrade = 0, int costUpgrade = 0)
        : base(cost, CardType.Power, rarity, TargetType.Self)
    {
        WithPower<T>(amount, upgrade);
        WithKeywords(GolemKeywords.Part);
        if (costUpgrade != 0) WithCostUpgradeBy(costUpgrade);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await PartCmd.Install<T>(ctx, this, DynamicVars.Power<T>().BaseValue);
}

/// <summary>Base for simple non-Part powers.</summary>
public abstract class PowerCard<T> : GolemCard where T : Golem.GolemCode.Powers.GolemPower
{
    protected PowerCard(int cost, CardRarity rarity, int amount, int upgrade = 0, int costUpgrade = 0)
        : base(cost, CardType.Power, rarity, TargetType.Self)
    {
        WithPower<T>(amount, upgrade);
        if (costUpgrade != 0) WithCostUpgradeBy(costUpgrade);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Buff<T>(ctx);
}

// =====================================================================
//  TOKEN
// =====================================================================

/// <summary>Token. Gain 3 Block. Exhaust. (Made by Crumble, Quarry.)</summary>
public sealed class Rubble : GolemCard
{
    public Rubble() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithBlock(3, 2);
        WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

// =====================================================================
//  COMMON
// =====================================================================

public sealed class Grind : GolemCard
{
    public Grind() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { WithDamage(5, 2); WithBlock(5, 2); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Guard(play); }
}

public sealed class ShoulderCharge : GolemCard
{
    public ShoulderCharge() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(9, 3); WithCards(1); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        if (PartCmd.Count(Owner.Creature) > 0) await DrawCards(ctx);
    }
}

public sealed class PileDriver : GolemCard
{
    public PileDriver() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, 4); WithPower<VulnerablePower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<VulnerablePower>(ctx, play); }
}

public sealed class Crumble : GolemCard
{
    public Crumble() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 3); WithTip(typeof(Rubble));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        await CardPileCmd.AddToCombatAndPreview<Rubble>(Owner.Creature, PileType.Hand, 1, Owner);
    }
}

public sealed class MudWall : GolemCard
{
    public MudWall() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithBlock(7, 3); WithPower<WeakPower>(1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await Debuff<WeakPower>(ctx, play); }
}

public sealed class Quarry : GolemCard
{
    public Quarry() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1); WithTip(typeof(Rubble));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DrawCards(ctx);
        await CardPileCmd.AddToCombatAndPreview<Rubble>(Owner.Creature, PileType.Hand, 1, Owner);
    }
}

public sealed class HeavySwing : GolemCard
{
    public HeavySwing() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { WithDamage(19, 6); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class ClayThrow : GolemCard
{
    public ClayThrow() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2); WithPower<WeakPower>(1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<WeakPower>(ctx, play); }
}

public sealed class Stonewall : GolemCard
{
    public Stonewall() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(12, 3); WithPower<PlatingPower>(2, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await Buff<PlatingPower>(ctx); }
}

public sealed class Tumble : GolemCard
{
    public Tumble() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(4, 2); WithVar("Repeat", 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

public sealed class Reinforce : GolemCard
{
    public Reinforce() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(5, 2, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 3);
        WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class IronGrip : GolemCard
{
    public IronGrip() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(7, 2, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 3);
        WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

// =====================================================================
//  UNCOMMON — Parts and powers
// =====================================================================

public sealed class MossyCrown() : PartCard<MossyCrownPower>(1, CardRarity.Uncommon, 3, 2);
public sealed class SpringLegs() : PartCard<SpringLegsPower>(1, CardRarity.Uncommon, 2, 1);
public sealed class IronHooks() : PartCard<IronHooksPower>(1, CardRarity.Uncommon, 1, 1);
public sealed class StompingLegs() : PartCard<StompingLegsPower>(2, CardRarity.Uncommon, 4, 2);
public sealed class StoneHeart() : PartCard<StoneHeartPower>(1, CardRarity.Uncommon, 1, 0, -1);
public sealed class MagmaCore() : PartCard<MagmaCorePower>(1, CardRarity.Uncommon, 3, 1);
public sealed class SpikedKnuckles() : PartCard<SpikedKnucklesPower>(1, CardRarity.Uncommon, 2, 1);
public sealed class RockSolid() : PowerCard<RockSolidPower>(1, CardRarity.Uncommon, 5, 2);
public sealed class AssemblyLine() : PowerCard<AssemblyLinePower>(1, CardRarity.Uncommon, 1, 0, -1);

// =====================================================================
//  UNCOMMON — skills and attacks
// =====================================================================

public sealed class Rebuild : GolemCard
{
    public Rebuild() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(6, 3); WithTip(typeof(ClayArms));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (!PartCmd.Has(Owner.Creature, PartSlot.Arms))
            await PartCmd.Install<ClayArmsPower>(ctx, this, IsUpgraded ? 3 : 2);
    }
}

public sealed class Overhaul : GolemCard
{
    public Overhaul() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("PerPart", 2); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var n = await PartCmd.RemoveAll(ctx, Owner.Creature);
        if (n > 0) await CardPileCmd.Draw(ctx, n * V("PerPart"), Owner);
    }
}

public sealed class Landslide : GolemCard
{
    public Landslide() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { WithDamage(9, 3); WithBlock(6, 2); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Guard(play); }
}

public sealed class BoulderRoll : GolemCard
{
    public BoulderRoll() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(11, 4); WithKeywords(CardKeyword.Retain);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class SeismicSlam : GolemCard
{
    public SeismicSlam() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(16, 4); WithPower<VulnerablePower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<VulnerablePower>(ctx, play); }
}

public sealed class Petrify : GolemCard
{
    public Petrify() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(2, 1); WithBlock(4, 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Debuff<WeakPower>(ctx, play); await Guard(play); }
}

public sealed class EarthenEmbrace : GolemCard
{
    public EarthenEmbrace() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { WithBlock(18, 6); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class Fortify : GolemCard
{
    public Fortify() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Base", 2, 1); WithVar("PerPart", 1); WithTip(typeof(PlatingPower)); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await Buff<PlatingPower>(ctx, V("Base") + V("PerPart") * PartCmd.Count(Owner.Creature));
}

public sealed class Rumble : GolemCard
{
    public Rumble() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 2); WithVar("Repeat", 2); WithBlock(3, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play, V("Repeat")); await Guard(play); }
}

public sealed class Upheaval : GolemCard
{
    public Upheaval() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithEnergy(2); WithVar("HpLoss", 4, -2); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await LoseHp(ctx, V("HpLoss"));
        await Energy(DynamicVars.Energy.BaseValue);
    }
}

public sealed class Mudbath : GolemCard
{
    public Mudbath() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithHeal(5, 3); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
}

public sealed class Slag : GolemCard
{
    public Slag() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3); WithPower<WeakPower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<WeakPower>(ctx, play); }
}

public sealed class Shatter : GolemCard
{
    public Shatter() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3); WithTip(typeof(VulnerablePower));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var hits = play.Target != null && play.Target.HasPower<VulnerablePower>() ? 2 : 1;
        await Hit(ctx, play, hits);
    }
}

public sealed class GravelStorm : GolemCard
{
    public GravelStorm() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
        WithDamage(3, 1); WithVar("Repeat", 5);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

public sealed class Brace : GolemCard
{
    public Brace() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(5, 2); WithPower<BlockNextTurnPower>(5, 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await Buff<BlockNextTurnPower>(ctx); }
}

public sealed class ClayShield : GolemCard
{
    public ClayShield() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(9, 3); WithVar("Extra", 4, 1); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (PartCmd.Has(Owner.Creature, PartSlot.Arms)) await GainBlock(V("Extra"), play);
    }
}

public sealed class ForgeAhead : GolemCard
{
    public ForgeAhead() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, 1); WithEnergy(1); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DrawCards(ctx);
        if (PartCmd.Count(Owner.Creature) >= 3) await Energy(DynamicVars.Energy.BaseValue);
    }
}

public sealed class Earthquake : GolemCard
{
    public Earthquake() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(16, 5); WithPower<WeakPower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await DebuffAll<WeakPower>(ctx); }
}

public sealed class Haymaker : GolemCard
{
    public Haymaker() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { WithDamage(22, 7); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Co-op only. ALL players gain 10 Block.</summary>
public sealed class StoneShelter : GolemCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public StoneShelter() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { WithBlock(10, 4); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var ally in Allies) await CreatureCmd.GainBlock(ally, DynamicVars.Block, play);
    }
}

// =====================================================================
//  RARE — Parts and powers
// =====================================================================

public sealed class CrystalSkull() : PartCard<CrystalSkullPower>(2, CardRarity.Rare, 2, 0, -1);
public sealed class ThinkingStone() : PartCard<ThinkingStonePower>(1, CardRarity.Rare, 2, 1);
public sealed class HammerArms() : PartCard<HammerArmsPower>(2, CardRarity.Rare, 6, 2);
public sealed class IronTreads() : PartCard<IronTreadsPower>(2, CardRarity.Rare, 3, 1);
public sealed class UnmovableLegs : PartCard<UnmovableLegsPower>
{
    public UnmovableLegs() : base(3, CardRarity.Rare, 1, 0, -1) => WithTip(typeof(BarricadePower));
}
public sealed class GeodeCore() : PartCard<GeodeCorePower>(3, CardRarity.Rare, 1, 0, -1);
public sealed class VolcanicCore() : PartCard<VolcanicCorePower>(2, CardRarity.Rare, 3, 1);
public sealed class PerfectForm() : PowerCard<PerfectFormPower>(2, CardRarity.Rare, 1, 0, -1);
public sealed class LivingFortress() : PowerCard<LivingFortressPower>(3, CardRarity.Rare, 1, 0, -1);
public sealed class SelfRepair() : PowerCard<SelfRepairPower>(1, CardRarity.Rare, 2, 1);

// =====================================================================
//  RARE — skills and attacks
// =====================================================================

public sealed class ColossusSlam : GolemCard
{
    public ColossusSlam() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(24, 6, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 6, bonusUpgrade: 2);
        WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class Reassemble : GolemCard
{
    public Reassemble() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("PerPart", 1); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithEnergyTip(); WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var n = await PartCmd.RemoveAll(ctx, Owner.Creature);
        if (n > 0) await Energy(n * V("PerPart"));
    }
}

public sealed class Mountainfall : GolemCard
{
    public Mountainfall() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(24, 8); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class Impregnable : GolemCard
{
    public Impregnable() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithBlock(30, 10); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class Earthshaker : GolemCard
{
    public Earthshaker() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(12, 4); WithPower<WeakPower>(2, 1); WithPower<VulnerablePower>(2, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        await Debuff<WeakPower>(ctx, play);
        await Debuff<VulnerablePower>(ctx, play);
    }
}

public sealed class RockBottom : GolemCard
{
    public RockBottom() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(2); WithCards(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Owner.Creature.CurrentHp * 2 > Owner.Creature.MaxHp) return;
        await Energy(DynamicVars.Energy.BaseValue);
        await DrawCards(ctx);
    }
}

public sealed class Indestructible : GolemCard
{
    public Indestructible() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<IntangiblePower>(1); WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Buff<IntangiblePower>(ctx);
}

public sealed class CrushingWeight : GolemCard
{
    public CrushingWeight() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(5, 1, (card, _) => card.Owner.Creature.Block, upgrade: 5);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class Bedrock : GolemCard
{
    public Bedrock() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<StrengthPower>(3, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Buff<StrengthPower>(ctx);
}

public sealed class Monolith : GolemCard
{
    public Monolith() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCalculatedBlock(6, 6, (card, _) => PartCmd.Count(card.Owner.Creature), upgrade: 4, bonusUpgrade: 2);
        WithTip(GolemKeywords.Part);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Install Clay Arms, Rooted Legs, Mud Core and Lantern Eye. Exhaust.</summary>
public sealed class FinalForm : GolemCard
{
    public FinalForm() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1);
        WithTip(typeof(ClayArms)); WithTip(typeof(RootedLegs)); WithTip(typeof(MudCore)); WithTip(typeof(LanternEye));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PartCmd.Install<ClayArmsPower>(ctx, this, 2);
        await PartCmd.Install<RootedLegsPower>(ctx, this, 4);
        await PartCmd.Install<MudCorePower>(ctx, this, 2);
        await PartCmd.Install<LanternEyePower>(ctx, this, 1);
    }
}

public sealed class GolemSmash : GolemCard
{
    public GolemSmash() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(12, 4); WithVar("Repeat", 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

public sealed class StoneRain : GolemCard
{
    public StoneRain() : base(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
        WithDamage(5, 2); WithVar("Repeat", 6);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

/// <summary>Co-op only. ALL players gain 2 Plating.</summary>
public sealed class SharedFoundations : GolemCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public SharedFoundations() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithPower<PlatingPower>(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var ally in Allies) await CommonActions.Apply<PlatingPower>(ctx, ally, this);
    }
}
