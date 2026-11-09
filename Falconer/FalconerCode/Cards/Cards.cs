using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Falconer.FalconerCode.Falcon;
using Falconer.FalconerCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Falconer.FalconerCode.Cards;

public static class FalconerKeywords
{
    [CustomEnum("Errand")] [KeywordProperties(AutoKeywordPosition.None)] public static CardKeyword Errand;
    [CustomEnum("Perched")] [KeywordProperties(AutoKeywordPosition.None)] public static CardKeyword Perched;
    [CustomEnum("Alone")] [KeywordProperties(AutoKeywordPosition.None)] public static CardKeyword Alone;
    [CustomEnum("Recall")] [KeywordProperties(AutoKeywordPosition.None)] public static CardKeyword Recall;
}

// ================= Basic =================

public sealed class StrikeFalconer : FalconerCard
{
    public StrikeFalconer() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    { WithDamage(6, 3); WithTags(CardTag.Strike); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
}

public sealed class DefendFalconer : FalconerCard
{
    public DefendFalconer() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    { WithBlock(5, 3); WithTags(CardTag.Defend); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Send Hunt (+4 damage when upgraded).</summary>
public sealed class LooseTheHawk : FalconerCard
{
    public LooseTheHawk() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    { WithVar("Extra", 0, 4); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Hunt, play, flat: V("Extra"));
}

/// <summary>Recall. Draw 1(2). Exhaust.</summary>
public sealed class Whistle : FalconerCard
{
    public Whistle() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    { WithCards(1, 1); WithKeywords(CardKeyword.Exhaust); WithTip(FalconerKeywords.Recall); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Recall(ctx); await DrawCards(ctx); }
}

// ================= Common =================

public sealed class JessStrike : FalconerCard
{
    public JessStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithCalculatedDamage(8, 3, PerchedOf, upgrade: 3); WithTags(CardTag.Strike); WithTip(FalconerKeywords.Perched); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
}

public sealed class GauntletGuard : FalconerCard
{
    public GauntletGuard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithCalculatedBlock(7, 3, PerchedOf, upgrade: 3); WithTip(FalconerKeywords.Perched); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class SendAfield : FalconerCard
{
    public SendAfield() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyAlly)
    { WithBlock(3, 2); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await GainBlock(DynamicVars.Block.BaseValue, play); await Send(ctx, Errand.Scout, play); }
}

public sealed class QuickRelease : FalconerCard
{
    public QuickRelease() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyAlly)
    { WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Send(ctx, Errand.Hunt, play);
}

public sealed class LoneHunter : FalconerCard
{
    public LoneHunter() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(7, 2); WithTip(FalconerKeywords.Alone); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play, Alone ? 2 : 1);
}

public sealed class Lure : FalconerCard
{
    public Lure() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithBlock(6, 3); WithTip(FalconerKeywords.Recall); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Guard(play); await Recall(ctx); }
}

public sealed class FetchCard : FalconerCard
{
    public FetchCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyAlly)
    { WithCards(1, 1); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await DrawCards(ctx); await Send(ctx, Errand.Fetch, play); }
}

public sealed class FeatherStep : FalconerCard
{
    public FeatherStep() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithBlock(5, 2); WithCards(1); WithTip(FalconerKeywords.Alone); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Guard(play); await Draw(ctx, Alone ? 2 : 1); }
}

public sealed class Hooded : FalconerCard
{
    public Hooded() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithEnergy(1); WithKeywords(CardKeyword.Exhaust); WithKeyword(CardKeyword.Retain, UpgradeType.Add); WithTip(FalconerKeywords.Perched); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { if (Perched) await Energy(V("Energy")); }
}

public sealed class Tracking : FalconerCard
{
    public Tracking() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(6, 2); WithPower<WeakPower>(1, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Hit(ctx, play); await Debuff<WeakPower>(ctx, play); }
}

public sealed class GloveBash : FalconerCard
{
    public GloveBash() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(12, 4); WithTip(FalconerKeywords.Perched); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Perched ? HitAll(ctx) : Hit(ctx, play);
}

public sealed class FieldRations : FalconerCard
{
    public FieldRations() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithBlock(6, 2); WithVar("Pct", 25, 15); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (FalconCmd.Away(Me) is { } a) a.ExtraPct += V("Pct");
    }
}

public sealed class HawkCall : FalconerCard
{
    public HawkCall() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(4, 2); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Hit(ctx, play); await Send(ctx, Errand.Harry, harry: play.Target); }
}

public sealed class Brace : FalconerCard
{
    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithCalculatedBlock(8, 3, AloneOf, upgrade: 3); WithTip(FalconerKeywords.Alone); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class FlushOut : FalconerCard
{
    public FlushOut() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    { WithDamage(5, 2); WithTip(FalconerKeywords.Alone); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await HitAll(ctx); if (Alone) await HitAll(ctx); }
}

public sealed class MewsCleanup : FalconerCard
{
    public MewsCleanup() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { WithCards(1, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardSelectCmd.FromHandForDiscard(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c != this, this);
        await DrawCards(ctx);
    }
}

public sealed class TiercelFeint : FalconerCard
{
    public TiercelFeint() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(6, 3); WithTip(FalconerKeywords.Errand); }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        Hit(ctx, play, FalconCmd.Away(Me) is { Amount: 1 } ? 2 : 1);
}

public sealed class Bells : FalconerCard
{
    public Bells() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithBlock(5, 2); WithTip(FalconerKeywords.Errand); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (FalconCmd.Away(Me) is { } a) await a.ChangeTurns(ctx, -1);
    }
}

public sealed class CreanceLine : FalconerCard
{
    public CreanceLine() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { WithDamage(9, 3); WithTip(FalconerKeywords.Recall); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { await Hit(ctx, play); await Recall(ctx); }
}

public sealed class OpenCountry : FalconerCard
{
    public OpenCountry() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { WithCards(2, 1); WithTip(FalconerKeywords.Perched); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var perched = Perched && !Away;
        await DrawCards(ctx);
        if (perched) await Send(ctx, Errand.Scout);
    }
}
