using Falconer.FalconerCode.Falcon;
using Falconer.FalconerCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Falconer.FalconerCode.Potions;

/// <summary>Common. The falcon returns now; its Errand resolves at 100%.</summary>
public sealed class HawkTreat : FalconerPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override Task OnUse(PlayerChoiceContext ctx, Creature? target) => FalconCmd.ReturnNow(ctx, Owner.Creature, 100);
}

/// <summary>Uncommon. This combat, Errands are 50% stronger.</summary>
public sealed class MoltingOil : FalconerPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        PowerCmd.Apply<MoltPower>(ctx, Owner.Creature, 50, Owner.Creature, null);
}

/// <summary>Rare. Hunt, Scout and Fetch resolve right now at 100%.</summary>
public sealed class WildCastDraught : FalconerPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
    {
        foreach (var e in new[] { Errand.Hunt, Errand.Scout, Errand.Fetch })
            await FalconCmd.ResolveWithRepeats(ctx, Owner.Creature, e, 100, Owner.Creature, null);
    }
}
