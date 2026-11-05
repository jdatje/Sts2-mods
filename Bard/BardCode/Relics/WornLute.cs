using MegaCrit.Sts2.Core.Entities.Relics;

namespace Bard.BardCode.Relics;

/// <summary>Starter relic. Your first Song each combat starts with 3 extra Verses.</summary>
public sealed class WornLute : BardRelic, Bard.BardCode.Songs.IFirstSongBonus
{
    public const int BonusVerses = 3;
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public override MegaCrit.Sts2.Core.Models.RelicModel GetUpgradeReplacement() =>
        MegaCrit.Sts2.Core.Models.ModelDb.Relic<MasterLute>();

    /// <summary>Called when a Song starts: the bonus Verses the first time each combat, otherwise 0.</summary>
    public int TryUse()
    {
        if (_usedThisCombat) return 0;
        _usedThisCombat = true;
        Flash();
        return BonusVerses;
    }
}
