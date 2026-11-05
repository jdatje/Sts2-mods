using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Golem.GolemCode.Cards;
using Golem.GolemCode.Extensions;
using Golem.GolemCode.Parts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Golem.GolemCode.Relics;

/// <summary>Ancient (upgraded starter). The first Part you install each combat gives 10 Block and draws 2 cards.</summary>
[Pool(typeof(SharedRelicPool))]
public sealed class HeartOfStone : CustomRelicModel, IPartListener
{
    public const int BlockAmount = 10, DrawAmount = 2;
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "heart_of_stone.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "heart_of_stone_outline.png".RelicImagePath();
    protected override string BigIconPath => "heart_of_stone.png".BigRelicImagePath();

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public async Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part)
    {
        if (_usedThisCombat || part.Owner != Owner.Creature) return;
        _usedThisCombat = true;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, BlockAmount, ValueProp.Unpowered, null);
        await CardPileCmd.Draw(ctx, DrawAmount, Owner);
    }
}

/// <summary>Common. While you have an Arms Part, your Attacks deal 1 additional damage.</summary>
public sealed class MasonsChisel : GolemRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature || cardSource == null || !props.IsPoweredAttack()) return 0m;
        return PartCmd.Has(Owner.Creature, PartSlot.Arms) ? 1m : 0m;
    }
}

/// <summary>Uncommon. At the start of each combat, the first Part in your draw pile is installed for free.</summary>
public sealed class SpareParts : GolemRelic
{
    private bool _doneThisCombat;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task BeforeCombatStart()
    {
        _doneThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (_doneThisCombat || player != Owner) return;
        _doneThisCombat = true;
        var part = Owner.PlayerCombatState?.DrawPile.Cards.FirstOrDefault(c => c.Keywords.Contains(GolemKeywords.Part));
        if (part == null) return;
        Flash();
        await CardCmd.AutoPlay(ctx, part, null);
    }
}

/// <summary>Rare. At the start of your turn, a random installed Part gains 1 (at most 5 per combat).</summary>
public sealed class LivingClay : GolemRelic
{
    public const int MaxPerCombat = 5;
    private int _gained;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task BeforeCombatStart()
    {
        _gained = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner || _gained >= MaxPerCombat) return;
        var parts = Owner.Creature.Powers.OfType<PartPower>().ToList();
        if (parts.Count == 0) return;
        var pick = parts[Owner.RunState.Rng.CombatCardGeneration.NextInt(0, parts.Count)];
        _gained++;
        Flash();
        await PowerCmd.ModifyAmount(ctx, pick, 1, Owner.Creature, null);
    }
}

/// <summary>Shop. You have an extra Core slot (checked by PartCmd.Capacity).</summary>
public sealed class SpareSocket : GolemRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
}
