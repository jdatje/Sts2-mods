using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CardSharp.CardSharpCode.Potions;

/// <summary>Common. Add a Joker into your hand.</summary>
public sealed class WildSpirits : CardSharpPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await CardPileCmd.AddToCombatAndPreview<Joker>(Owner.Creature, PileType.Hand, 1, Owner);
}

/// <summary>Uncommon. Showdown now.</summary>
public sealed class FreshDeck : CardSharpPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target) =>
        await TableCmd.Showdown(ctx, Owner);
}

/// <summary>Rare. All cards in your hand become Aces.</summary>
public sealed class LiquidLuck : CardSharpPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
    {
        foreach (var c in Owner.PlayerCombatState?.Hand.Cards.OfType<CardSharpCard>().Where(c => !c.IsWild) ?? [])
            c.SetRank(Poker.MaxRank);
        return Task.CompletedTask;
    }
}
