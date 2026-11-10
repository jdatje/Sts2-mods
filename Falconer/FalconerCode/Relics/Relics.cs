using BaseLib.Abstracts;
using BaseLib.Utils;
using Falconer.FalconerCode.Cards;
using Falconer.FalconerCode.Character;
using Falconer.FalconerCode.Falcon;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Falconer.FalconerCode.Relics;

/// <summary>
/// The Falconer's starter relics (Falconer's Glove and its Ancient version Gilded Hood).
/// They hold the falcon's run state: XP (→ Level) and the picked Traits, saved with the run.
/// They also add the "Fly" option at rest sites and drive the on-screen falcon.
/// </summary>
public abstract class GloveRelic : Visuals.AnimatedStarterRelic
{
    /// <summary>Last known state per player, so the Ancient upgrade (Touch of Orobas) keeps the falcon's progress.</summary>
    private static readonly Dictionary<ulong, (int xp, int traits)> LastKnown = [];

    [SavedProperty] public int FalconXp { get; set; }
    [SavedProperty] public int FalconTraits { get; set; }

    // Per-combat flags (not saved).
    private bool _twinBroodUsed, _magpieUsed, _silverBellsUsed;
    public int ErrandsThisCombat { get; private set; }

    protected virtual int ExtraXpPerCombat => 0;
    protected virtual bool AlwaysTwinBrood => false;

    public override bool ShowCounter => true;
    public override int DisplayAmount => Level;

    /// <summary>XP needed to go from Level L to L+1 is 2 + L. Level 1–10.</summary>
    public static int LevelFor(int xp)
    {
        var level = 1;
        while (level < FalconCmd.MaxLevel && xp >= 2 + level) { xp -= 2 + level; level++; }
        return level;
    }

    public int Level => LevelFor(FalconXp);
    public bool HasTrait(Trait t) => (FalconTraits & (1 << (int)t)) != 0;
    public IEnumerable<Trait> MissingTraits => Enum.GetValues<Trait>().Where(t => !HasTrait(t));

    public void AddTrait(Trait t)
    {
        FalconTraits |= 1 << (int)t;
        Remember();
        InvokeDisplayAmountChanged();
    }

    public void AddXp(int xp)
    {
        if (xp <= 0) return;
        var before = Level;
        FalconXp += xp;
        Remember();
        if (Level != before) Flash();
        InvokeDisplayAmountChanged();
    }

    private void Remember()
    {
        if (Owner != null) LastKnown[Owner.NetId] = (FalconXp, FalconTraits);
    }

    /// <summary>When this relic replaces another Glove (Ancient upgrade), carry over its progress.</summary>
    public override Task AfterObtained()
    {
        var other = Owner.Relics.OfType<GloveRelic>().FirstOrDefault(r => r != this);
        if (other != null) { FalconXp = other.FalconXp; FalconTraits = other.FalconTraits; }
        else if (FalconXp == 0 && FalconTraits == 0 && LastKnown.TryGetValue(Owner.NetId, out var s))
        { FalconXp = s.xp; FalconTraits = s.traits; }
        Remember();
        return Task.CompletedTask;
    }

    public bool TakeTwinBrood()
    {
        if (_twinBroodUsed || !(AlwaysTwinBrood || HasTrait(Trait.TwinBrood))) return false;
        _twinBroodUsed = true;
        Flash();
        return true;
    }

    public bool TakeMagpie()
    {
        if (_magpieUsed) return false;
        _magpieUsed = true;
        return true;
    }

    public bool TakeSilverBells()
    {
        if (_silverBellsUsed || !Owner.Relics.OfType<SilverBells>().Any()) return false;
        _silverBellsUsed = true;
        return true;
    }

    public void CountErrand() => ErrandsThisCombat++;

    public override Task BeforeCombatStart()
    {
        _twinBroodUsed = _magpieUsed = _silverBellsUsed = false;
        ErrandsThisCombat = 0;
        Remember();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        await base.AfterPlayerTurnStart(ctx, player);
        if (player != Owner) return;
        FalconOverlay.Refresh(Owner.Creature);
        // Loyal: the first turn starts with a Hunt already in the air.
        if (HasTrait(Trait.Loyal) && Owner.Creature.CombatState?.RoundNumber == 1 && !FalconCmd.IsAway(Owner.Creature))
            await FalconCmd.Send(ctx, Owner.Creature, Errand.Hunt);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        var xp = room.RoomType switch
        {
            RoomType.Elite => 3,
            RoomType.Boss => 5,
            _ => 1,
        } + ExtraXpPerCombat;
        AddXp(xp);
    }

    public override async Task AfterCombatVictoryEarly(CombatRoom room)
    {
        await base.AfterCombatVictoryEarly(room);
        FalconOverlay.Hide(Owner.Creature);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        FalconOverlay.Hide(Owner.Creature);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || !MissingTraits.Any()) return false;
        options.Add(new FlyRestSiteOption(player));
        return true;
    }
}

/// <summary>Starter relic. Holds the falcon's Level and Traits.</summary>
[Pool(typeof(FalconerRelicPool))]
public sealed class FalconersGlove : GloveRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<GildedHood>();
}

/// <summary>Ancient (upgraded starter). As the Glove; the first Errand each combat resolves twice; +1 XP per combat.</summary>
[Pool(typeof(SharedRelicPool))]
public sealed class GildedHood : GloveRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override int ExtraXpPerCombat => 1;
    protected override bool AlwaysTwinBrood => true;
}

/// <summary>Common. Whenever you Recall, gain 4 Block.</summary>
public sealed class LeatherLure : FalconerRelic, IFalconListener
{
    public const int Block = 4;
    public override RelicRarity Rarity => RelicRarity.Common;

    public async Task AfterRecall(PlayerChoiceContext ctx)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, Block, ValueProp.Unpowered, null);
    }
}

/// <summary>Uncommon. Your first Errand of 2+ turns each combat is 1 turn faster (checked in FalconCmd.Send).</summary>
public sealed class SilverBells : FalconerRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

/// <summary>Shop. Whenever you enter a rest site, the falcon gains 2 XP.</summary>
public sealed class FalconPerch : FalconerRelic
{
    public const int Xp = 2;
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.RestSite)
        {
            Flash();
            FalconCmd.Glove(Owner)?.AddXp(Xp);
        }
        return Task.CompletedTask;
    }
}

/// <summary>Rest site option: pick 1 of 3 random Traits the falcon doesn't have yet.</summary>
public sealed class FlyRestSiteOption(Player owner) : CustomRestSiteOption(owner)
{
    public override string OptionId => "FALCONER_FLY";
    public override string? CustomIconPath => MainFile.ResPath + "/images/charui/rest_fly_falconer.png";

    public override async Task<bool> OnSelect()
    {
        var glove = FalconCmd.Glove(Owner);
        if (glove == null) return false;
        var missing = glove.MissingTraits.ToList();
        if (missing.Count == 0) return false;

        var rng = Owner.PlayerRng.Rewards;
        var offer = new List<Trait>();
        while (offer.Count < 3 && missing.Count > 0)
        {
            var pick = missing[rng.NextInt(0, missing.Count)];
            missing.Remove(pick);
            offer.Add(pick);
        }

        var cards = offer.Select(t => TraitCard.Create(t, Owner)).ToList();
        var chosen = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, Owner, canSkip: false);
        if (chosen is not TraitCard tc) return false;
        glove.AddTrait(tc.Trait);
        return true;
    }
}
