using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Bard.BardCode.Extensions;
using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Relics;

/// <summary>Ancient (upgraded starter). Your first Song each combat starts with 6 extra Verses.</summary>
[Pool(typeof(SharedRelicPool))]
public sealed class MasterLute : CustomRelicModel, IFirstSongBonus
{
    public const int BonusVerses = 6;
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "master_lute.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "master_lute_outline.png".RelicImagePath();
    protected override string BigIconPath => "master_lute.png".BigRelicImagePath();

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public int TryUse()
    {
        if (_usedThisCombat) return 0;
        _usedThisCombat = true;
        Flash();
        return BonusVerses;
    }
}

/// <summary>Common. Whenever you start a Song, gain 4 Block.</summary>
public sealed class SheetMusic : BardRelic, ISongListener
{
    public const int Block = 4;
    public override RelicRarity Rarity => RelicRarity.Common;

    public async Task AfterSongStarted(PlayerChoiceContext ctx, SongPower song)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, null);
    }

    public Task AfterFinale(PlayerChoiceContext ctx, SongPower song) => Task.CompletedTask;
}

/// <summary>Uncommon. Your Songs can have 3 more Verses (checked in SongCmd.MaxVerses).</summary>
public sealed class TuningFork : BardRelic
{
    public const int ExtraVerses = 3;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

/// <summary>Rare. While your Song has 10 or more Verses, its Refrain triggers an extra time (checked in SongPower).</summary>
public sealed class GoldenStrings : BardRelic
{
    public const int MinVerses = 10;
    public override RelicRarity Rarity => RelicRarity.Rare;
}

/// <summary>Shop. Whenever a Finale triggers, gain 5 Gold.</summary>
public sealed class BuskersHat : BardRelic, IFinaleTriggerListener
{
    public const int Gold = 5;
    public override RelicRarity Rarity => RelicRarity.Shop;

    public async Task AfterFinaleTrigger(PlayerChoiceContext ctx)
    {
        Flash();
        await PlayerCmd.GainGold(Gold, Owner);
    }
}
