using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CardSharp.CardSharpCode.Cards;

// ================= House Rules (shown at the rest site; never in a deck) =================

public abstract class HouseRuleCard(HouseRule rule) : CardSharpCard(0, CardType.Power, CardRarity.Token, TargetType.None)
{
    public override Suit Suit => Suit.Diamonds;
    protected override bool IsPlayable => false;
    public HouseRule Rule { get; } = rule;
    protected override Task Effect(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;

    public static HouseRuleCard Create(HouseRule r, Player owner) => r switch
    {
        HouseRule.MatchedSet => owner.RunState.CreateCard<RuleMatchedSet>(owner),
        HouseRule.FullTable => owner.RunState.CreateCard<RuleFullTable>(owner),
        HouseRule.RoyalCourt => owner.RunState.CreateCard<RuleRoyalCourt>(owner),
        HouseRule.AceHigh => owner.RunState.CreateCard<RuleAceHigh>(owner),
        HouseRule.Suited => owner.RunState.CreateCard<RuleSuited>(owner),
        HouseRule.ColourUp => owner.RunState.CreateCard<RuleColourUp>(owner),
        HouseRule.GapFiller => owner.RunState.CreateCard<RuleGapFiller>(owner),
        HouseRule.LongRun => owner.RunState.CreateCard<RuleLongRun>(owner),
        HouseRule.QuickDeal => owner.RunState.CreateCard<RuleQuickDeal>(owner),
        _ => owner.RunState.CreateCard<RuleShortStack>(owner),
    };
}

public sealed class RuleMatchedSet() : HouseRuleCard(HouseRule.MatchedSet);
public sealed class RuleFullTable() : HouseRuleCard(HouseRule.FullTable);
public sealed class RuleRoyalCourt() : HouseRuleCard(HouseRule.RoyalCourt);
public sealed class RuleAceHigh() : HouseRuleCard(HouseRule.AceHigh);
public sealed class RuleSuited() : HouseRuleCard(HouseRule.Suited);
public sealed class RuleColourUp() : HouseRuleCard(HouseRule.ColourUp);
public sealed class RuleGapFiller() : HouseRuleCard(HouseRule.GapFiller);
public sealed class RuleLongRun() : HouseRuleCard(HouseRule.LongRun);
public sealed class RuleQuickDeal() : HouseRuleCard(HouseRule.QuickDeal);
public sealed class RuleShortStack() : HouseRuleCard(HouseRule.ShortStack);
