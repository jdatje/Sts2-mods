using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Character;
using CardSharp.CardSharpCode.Extensions;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace CardSharp.CardSharpCode.Relics;

/// <summary>
/// The Card Sharp's starter relics (Marked Deck and its Ancient version Stacked Deck).
/// The Pot: each Showdown deals 2 more damage than the last, this combat, up to a cap.
/// They also keep the Table overlay shown, placed and cleared.
/// </summary>
public abstract class PotRelic : CardSharp.CardSharpCode.Visuals.AnimatedStarterRelic, IShowdownPot
{
    public const int PotStep = 2;
    private int _showdowns;

    protected abstract int PotCap { get; }


    public int CurrentPot => Math.Min(PotCap, PotStep * _showdowns);

    public int TakePot()
    {
        var pot = CurrentPot;
        _showdowns++;
        if (pot > 0) Flash();
        return pot;
    }

    /// <summary>Shows the (empty) Table panel from the first turn on, and keeps it placed above her.</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner) TableOverlay.Refresh(Owner.Creature);
    }

    /// <summary>Remove the Table panel as soon as the fight is won, so it doesn't linger on the map.</summary>
    public override async Task AfterCombatVictoryEarly(CombatRoom room)
    {
        await base.AfterCombatVictoryEarly(room);
        TableOverlay.Hide(Owner.Creature);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        TableCmd.Clear(Owner.Creature);
        TableOverlay.Hide(Owner.Creature);
    }

    public override Task BeforeCombatStart()
    {
        _showdowns = 0;
        TableCmd.Clear(Owner.Creature);
        return Task.CompletedTask;
    }
}

/// <summary>Starter relic. The Pot grows +2 per Showdown, up to +7.</summary>
[Pool(typeof(CardSharpRelicPool))]
public sealed class MarkedDeck : PotRelic
{
    public const int Cap = 7;
    protected override int PotCap => Cap;
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<StackedDeck>();
}

/// <summary>Ancient (upgraded starter). The Pot grows +2 per Showdown, up to +10.</summary>
[Pool(typeof(SharedRelicPool))]
public sealed class StackedDeck : PotRelic
{
    public const int Cap = 10;
    protected override int PotCap => Cap;
    public override RelicRarity Rarity => RelicRarity.Ancient;
}
