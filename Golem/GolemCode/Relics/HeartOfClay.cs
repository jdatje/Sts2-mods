using Golem.GolemCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Golem.GolemCode.Parts;

namespace Golem.GolemCode.Relics;

/// <summary>Starter relic. The first Part you install each combat also gives 6 Block.</summary>
public sealed class HeartOfClay : GolemRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override MegaCrit.Sts2.Core.Models.RelicModel GetUpgradeReplacement() =>
        MegaCrit.Sts2.Core.Models.ModelDb.Relic<HeartOfStone>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Unpowered)];

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisCombat) return;
        if (cardPlay.Card.Owner != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(GolemKeywords.Part)) return;

        _usedThisCombat = true;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
