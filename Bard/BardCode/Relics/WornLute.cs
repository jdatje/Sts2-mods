using MegaCrit.Sts2.Core.Entities.Relics;

namespace Bard.BardCode.Relics;

/// <summary>Starter relic. Your first Song each combat starts with 3 extra Verses.</summary>
public sealed class WornLute : BardRelic
{
    public const int BonusVerses = 3;
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public bool TryUse()
    {
        if (_usedThisCombat) return false;
        _usedThisCombat = true;
        Flash();
        return true;
    }
}
