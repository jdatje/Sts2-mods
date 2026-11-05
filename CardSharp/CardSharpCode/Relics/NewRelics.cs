using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CardSharp.CardSharpCode.Relics;

/// <summary>Common. Your first Showdown each combat counts as 1 hand higher.</summary>
public sealed class LuckyCoin : CardSharpRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    /// <summary>Called by TableCmd.Showdown. True (and flashes) the first time each combat.</summary>
    public bool TryUse()
    {
        if (_usedThisCombat) return false;
        _usedThisCombat = true;
        Flash();
        return true;
    }
}

/// <summary>Uncommon. Whenever you Showdown, draw 1 card.</summary>
public sealed class DealersButton : CardSharpRelic, IShowdownListener
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        Flash();
        await CardPileCmd.Draw(ctx, 1, Owner);
    }
}

/// <summary>Rare. At the start of each combat, shuffle 2 Jokers into your draw pile.</summary>
public sealed class GoldenJoker : CardSharpRelic
{
    public const int Count = 2;
    private bool _doneThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task BeforeCombatStart()
    {
        _doneThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (_doneThisCombat || player != Owner) return;
        _doneThisCombat = true;
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Joker>(Owner.Creature, PileType.Draw, Count, Owner, CardPilePosition.Random);
    }
}

/// <summary>Shop. Whenever you Showdown, gain 2 Gold.</summary>
public sealed class CroupiersRake : CardSharpRelic, IShowdownListener
{
    public const int Gold = 2;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public async Task AfterShowdown(PlayerChoiceContext ctx, HandType hand)
    {
        Flash();
        await PlayerCmd.GainGold(Gold, Owner);
    }
}
