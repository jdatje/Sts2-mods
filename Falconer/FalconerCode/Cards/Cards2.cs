using BaseLib.Abstracts;
using Falconer.FalconerCode.Falcon;
using Falconer.FalconerCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Falconer.FalconerCode.Cards;

// ================= Uncommon =================

public sealed class Stoop : FalconerCard
{
    public Stoop() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithCostUpgradeBy(-1); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Stoop);
}

public sealed class WaitForIt : FalconerCard
{
    public WaitForIt() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithCalculatedDamage(6, 8, AloneOf, upgrade: 4); WithKeywords(CardKeyword.Retain); WithTip(FalconerKeywords.Alone); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
}

public sealed class FalconsShare : FalconerPowerCard<FalconsSharePower>
{
    public FalconsShare() : base(1, CardRarity.Uncommon, 1, 1) { }
}

public sealed class LongFlight : FalconerCard
{
    public LongFlight() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithVar("Pct", 50, 25); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (FalconCmd.Away(Me) is not { } a) return;
        a.ExtraPct += V("Pct");
        await a.ChangeTurns(ctx, +1);
    }
}

public sealed class RingingUp : FalconerCard
{
    public RingingUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { WithVar("Extra", 4, 3); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Hunt, play, flat: V("Extra"));
}

public sealed class GloveUp : FalconerPowerCard<GloveUpPower>
{
    public GloveUp() : base(1, CardRarity.Uncommon, 4, 2) { }
}

public sealed class HuntersPatience : FalconerPowerCard<HuntersPatiencePower>
{
    public HuntersPatience() : base(1, CardRarity.Uncommon, 3, 1) { }
}

public sealed class SharedQuarry : FalconerCard
{
    public SharedQuarry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { WithCards(1, 1); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Send(ctx, Errand.Harry, play);
        await DrawCards(ctx);
        if (play.Target?.Player is { } ally && ally != Owner) await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(ctx, V("Cards"), ally);
    }
}

public sealed class DiveBomb : FalconerCard
{
    public DiveBomb() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(10, 6, (c, _) => FalconCmd.Glove(c.Owner)?.ErrandsThisCombat ?? 0, upgrade: 4);
        WithTip(FalconerKeywords.Errand);
    }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
}

public sealed class Mantle : FalconerCard
{
    public Mantle() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithBlock(11, 4); WithKeywords(CardKeyword.Retain); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class Recon : FalconerCard
{
    public Recon() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { WithVar("Extra", 1, 1); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Scout, play, flat: V("Extra"));
}

public sealed class CastOff : FalconerCard
{
    public CastOff() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithBlock(10, 4); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Guard(play); await Send(ctx, Errand.Stoop); }
}

public sealed class Pellet : FalconerCard
{
    public Pellet() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => FalconCmd.ReturnNow(ctx, Me, 100);
}

public sealed class Imprint : FalconerPowerCard<ImprintPower>
{
    public Imprint() : base(2, CardRarity.Uncommon, 20, 10) { }
}

public sealed class WindReading : FalconerCard
{
    public WindReading() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithCards(2, 1); WithTip(FalconerKeywords.Alone); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Draw(ctx, V("Cards") + (Alone ? 1 : 0));
}

public sealed class TalonRake : FalconerCard
{
    public TalonRake() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithDamage(3, 1); WithTip(FalconerKeywords.Perched); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play, Perched ? 4 : 3);
}

public sealed class HawkBells : FalconerPowerCard<HawkBellsPower>
{
    public HawkBells() : base(1, CardRarity.Uncommon, 3, 2) { }
}

public sealed class Bate : FalconerCard
{
    public Bate() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithDamage(8, 3); WithEnergy(1); WithTip(FalconerKeywords.Recall); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Hit(ctx, play); await Recall(ctx); await Energy(V("Energy")); }
}

public sealed class HawkEye : FalconerCard
{
    public HawkEye() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithPower<VulnerablePower>(2, 1); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Debuff<VulnerablePower>(ctx, play); await Send(ctx, Errand.Scout); }
}

public sealed class Wingover : FalconerCard
{
    public Wingover() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { WithVar("Pct", 100); WithCostUpgradeBy(-1); WithKeywords(CardKeyword.Exhaust); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (FalconCmd.Away(Me) is { } a) a.ExtraPct += V("Pct");
        return Task.CompletedTask;
    }
}

public sealed class FlightFeathers : FalconerPowerCard<FlightFeathersPower>
{
    public FlightFeathers() : base(1, CardRarity.Uncommon, 1, 0, -1) { }
}

public sealed class HackBack : FalconerCard
{
    public HackBack() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithDamage(10, 3); WithTip(FalconerKeywords.Recall); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Recall(ctx); await Hit(ctx, play); }
}

public sealed class Partners : FalconerPowerCard<PartnersPower>
{
    public Partners() : base(2, CardRarity.Uncommon, 1, 0, -1) { WithTip(FalconerKeywords.Perched); WithTip(FalconerKeywords.Alone); }
}

public sealed class QuarryMark : FalconerCard
{
    public QuarryMark() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    { WithPower<QuarryMarkPower>(50, 50); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Debuff<QuarryMarkPower>(ctx, play);
}

public sealed class Lofting : FalconerCard
{
    public Lofting() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { WithCostUpgradeBy(-1); WithKeywords(CardKeyword.Exhaust); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Forage, play);
}

public sealed class Weathered : FalconerPowerCard<WeatheredPower>
{
    public Weathered() : base(1, CardRarity.Uncommon, 5, 2) { }
}

// ================= Rare =================

public sealed class Peregrine : FalconerPowerCard<PeregrinePower>
{
    public Peregrine() : base(3, CardRarity.Rare, 1, 0, -1) { WithTip(FalconerKeywords.Errand); }
}

public sealed class Apex : FalconerCard
{
    public Apex() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { WithCalculatedDamage(24, 3, (c, _) => FalconCmd.Level(c.Owner), upgrade: 8); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
}

public sealed class SkySovereign : FalconerPowerCard<SkySovereignPower>
{
    public SkySovereign() : base(2, CardRarity.Rare, 1, 0, -1) { }
}

public sealed class StoopAndRise : FalconerCard
{
    public StoopAndRise() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithCostUpgradeBy(-1); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Stoop, chain: true);
}

public sealed class Eyas : FalconerCard
{
    public Eyas() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithVar("Xp", 2, 1); WithKeywords(CardKeyword.Exhaust); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        FalconCmd.Glove(Owner)?.AddXp(V("Xp"));
        return Task.CompletedTask;
    }
}

public sealed class FalconersPact : FalconerPowerCard<FalconersPactPower>
{
    public FalconersPact() : base(2, CardRarity.Rare, 1, 0, -1) { }
}

public sealed class Hoodwink : FalconerCard
{
    public Hoodwink() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithCostUpgradeBy(-1); WithKeywords(CardKeyword.Retain); WithTip(FalconerKeywords.Recall); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Recall(ctx, 100);
}

public sealed class Murmuration : FalconerCard
{
    public Murmuration() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    { WithPower<WeakPower>(2, 1); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await DebuffAll<WeakPower>(ctx); await Send(ctx, Errand.Harry); }
}

public sealed class GrandCast : FalconerCard
{
    public GrandCast() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithVar("Pct", 50, 25); WithKeywords(CardKeyword.Exhaust); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var e in new[] { Errand.Hunt, Errand.Scout, Errand.Fetch })
            await FalconCmd.ResolveWithRepeats(ctx, Me, e, V("Pct"), Me, null);
    }
}

public sealed class LoneWolf : FalconerPowerCard<LoneWolfPower>
{
    public LoneWolf() : base(1, CardRarity.Rare, 1, 0, -1) { WithTip(FalconerKeywords.Alone); }
}

public sealed class BondOfTheHunt : FalconerPowerCard<BondOfTheHuntPower>
{
    public BondOfTheHunt() : base(1, CardRarity.Rare, 2, 1) { WithTip(FalconerKeywords.Perched); }
}

public sealed class Rapture : FalconerCard
{
    public Rapture() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    { WithDamage(12, 4); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await HitAll(ctx); await Send(ctx, Errand.Hunt); }
}

public sealed class NightFlight : FalconerCard
{
    public NightFlight() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithCostUpgradeBy(-1); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (FalconCmd.Away(Me) is not { } a) return;
        var (errand, forC, harry) = (a.Errand, a.For, a.HarryTarget);
        await FalconCmd.ReturnNow(ctx, Me, 100);
        await FalconCmd.Send(ctx, Me, errand, forC, harry);
    }
}

public sealed class HoodAndJess : FalconerCard
{
    public HoodAndJess() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithEnergy(2, 1); WithKeywords(CardKeyword.Exhaust); WithTip(FalconerKeywords.Perched); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { if (Perched) await Energy(V("Energy")); }
}

public sealed class WildHeart : FalconerPowerCard<WildHeartPower>
{
    public WildHeart() : base(2, CardRarity.Rare, 1, 0, -1) { }
}

public sealed class Trophy : FalconerCard
{
    public Trophy() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { WithDamage(10, 4); WithVar("Xp", 1); WithKeywords(CardKeyword.Exhaust); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        if (play.Target is { IsAlive: false }) FalconCmd.Glove(Owner)?.AddXp(V("Xp"));
    }
}

public sealed class Gyrfalcon : FalconerPowerCard<GyrfalconPower>
{
    public Gyrfalcon() : base(2, CardRarity.Rare, 4, 2) { WithTip(FalconerKeywords.Errand); }
}

public sealed class LastLight : FalconerCard
{
    public LastLight() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { WithDamage(25, 7); WithTip(FalconerKeywords.Alone); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Alone ? HitAll(ctx) : Hit(ctx, play);
}

// ================= Traits (shown at the rest site; never in a deck) =================

public abstract class TraitCard(Trait trait) : FalconerCard(0, CardType.Power, CardRarity.Token, TargetType.None)
{
    protected override bool IsPlayable => false;
    public Trait Trait { get; } = trait;
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;

    public static TraitCard Create(Trait t, Player owner) => t switch
    {
        Trait.SharpTalons => owner.RunState.CreateCard<TraitSharpTalons>(owner),
        Trait.TwinBrood => owner.RunState.CreateCard<TraitTwinBrood>(owner),
        Trait.KeenEye => owner.RunState.CreateCard<TraitKeenEye>(owner),
        Trait.SwiftWings => owner.RunState.CreateCard<TraitSwiftWings>(owner),
        Trait.Homing => owner.RunState.CreateCard<TraitHoming>(owner),
        Trait.Loyal => owner.RunState.CreateCard<TraitLoyal>(owner),
        Trait.ThickPlumage => owner.RunState.CreateCard<TraitThickPlumage>(owner),
        Trait.Magpie => owner.RunState.CreateCard<TraitMagpie>(owner),
        Trait.TerrorOfTheSkies => owner.RunState.CreateCard<TraitTerrorOfTheSkies>(owner),
        Trait.Patient => owner.RunState.CreateCard<TraitPatient>(owner),
        Trait.Bonded => owner.RunState.CreateCard<TraitBonded>(owner),
        _ => owner.RunState.CreateCard<TraitWideRange>(owner),
    };
}

public sealed class TraitSharpTalons() : TraitCard(Trait.SharpTalons);
public sealed class TraitTwinBrood() : TraitCard(Trait.TwinBrood);
public sealed class TraitKeenEye() : TraitCard(Trait.KeenEye);
public sealed class TraitSwiftWings() : TraitCard(Trait.SwiftWings);
public sealed class TraitHoming() : TraitCard(Trait.Homing);
public sealed class TraitLoyal() : TraitCard(Trait.Loyal);
public sealed class TraitThickPlumage() : TraitCard(Trait.ThickPlumage);
public sealed class TraitMagpie() : TraitCard(Trait.Magpie);
public sealed class TraitTerrorOfTheSkies() : TraitCard(Trait.TerrorOfTheSkies);
public sealed class TraitPatient() : TraitCard(Trait.Patient);
public sealed class TraitBonded() : TraitCard(Trait.Bonded);
public sealed class TraitWideRange() : TraitCard(Trait.WideRange);
