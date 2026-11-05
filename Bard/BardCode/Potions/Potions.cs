using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Bard.BardCode.Potions;

/// <summary>Common. Add 5 Verses to your Song.</summary>
public sealed class ThroatCoat : BardPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await SongCmd.AddVerses(ctx, Owner.Creature, 5);
}

/// <summary>Uncommon. Your Song's Refrain triggers twice.</summary>
public sealed class EncoreDraught : BardPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
    {
        var song = SongCmd.Current(Owner.Creature);
        if (song == null) return;
        await song.Refrain(ctx);
        await song.Refrain(ctx);
    }
}

/// <summary>Rare. Your Song's Finale triggers, without ending the Song.</summary>
public sealed class OvertureElixir : BardPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await SongCmd.Finale(ctx, Owner.Creature, 1);
}
