using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace CardSharp.CardSharpCode.Relics;

/// <summary>Starter relic. Your first Showdown each combat counts as one hand higher.</summary>
public sealed class MarkedDeck : CardSharpRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>Shows the (empty) Table panel from the first turn on, and keeps it placed above her.</summary>
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) TableOverlay.Refresh(Owner.Creature);
        return Task.CompletedTask;
    }

    /// <summary>Remove the Table panel as soon as the fight is won, so it doesn't linger on the map.</summary>
    public override Task AfterCombatVictoryEarly(CombatRoom room)
    {
        TableOverlay.Hide(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        TableOverlay.Hide(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    /// <summary>Called by TableCmd.Showdown. Returns true (and flashes) the first time each combat.</summary>
    public bool TryUse()
    {
        if (_usedThisCombat) return false;
        _usedThisCombat = true;
        Flash();
        return true;
    }
}
