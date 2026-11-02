using MegaCrit.Sts2.Core.Entities.Relics;

namespace CardSharp.CardSharpCode.Relics;

/// <summary>Starter relic. Your first Showdown each combat counts as one hand higher.</summary>
public sealed class MarkedDeck : CardSharpRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

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
