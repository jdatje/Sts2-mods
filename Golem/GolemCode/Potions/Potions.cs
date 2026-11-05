using Golem.GolemCode.Cards;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Golem.GolemCode.Potions;

/// <summary>Common. Add 3 Rubble into your hand.</summary>
public sealed class QuarryBrew : GolemPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await CardPileCmd.AddToCombatAndPreview<Rubble>(Owner.Creature, PileType.Hand, 3, Owner);
}

/// <summary>Uncommon. Gain 6 Plating.</summary>
public sealed class MortarTonic : GolemPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await PowerCmd.Apply<PlatingPower>(ctx, Owner.Creature, 6, Owner.Creature, null);
}

/// <summary>Rare. Install a random Part from your draw pile for free (Clay Arms if there is none).</summary>
public sealed class LiquidClay : GolemPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
    {
        var parts = Owner.PlayerCombatState?.DrawPile.Cards.Where(c => c.Keywords.Contains(GolemKeywords.Part)).ToList() ?? [];
        if (parts.Count > 0)
        {
            var pick = parts[Owner.RunState.Rng.CombatCardGeneration.NextInt(0, parts.Count)];
            await CardCmd.AutoPlay(ctx, pick, null);
            return;
        }
        await PartCmd.Install<ClayArmsPower>(ctx, Owner.Creature, 1, null);
    }
}
