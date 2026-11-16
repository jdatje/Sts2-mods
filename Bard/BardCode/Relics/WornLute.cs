using MegaCrit.Sts2.Core.Entities.Relics;

namespace Bard.BardCode.Relics;

/// <summary>Starter relic. Your first Song each combat starts with 3 extra Verses. Holds Song Mastery, Stanzas and Harmonies.</summary>
[BaseLib.Utils.Pool(typeof(Bard.BardCode.Character.BardRelicPool))]
public sealed class WornLute : LuteRelic
{
    public const int Bonus = 3;
    protected override int BonusVerses => Bonus;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override MegaCrit.Sts2.Core.Models.RelicModel GetUpgradeReplacement() =>
        MegaCrit.Sts2.Core.Models.ModelDb.Relic<MasterLute>();
}
