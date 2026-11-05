using BaseLib.Extensions;
using BaseLib.Utils;
using Bard.BardCode.Powers;
using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Cards;

/// <summary>Base for simple Bard power cards.</summary>
public abstract class BardPowerCard<T> : BardCard where T : BardPower
{
    protected BardPowerCard(int cost, CardRarity rarity, int amount, int upgrade = 0, int costUpgrade = 0)
        : base(cost, CardType.Power, rarity, TargetType.Self)
    {
        WithPower<T>(amount, upgrade);
        if (costUpgrade != 0) WithCostUpgradeBy(costUpgrade);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Buff<T>(ctx);
}

static class Song
{
    public static int VersesOf(MegaCrit.Sts2.Core.Models.CardModel card) => SongCmd.Verses(card.Owner.Creature);
}

// =====================================================================
//  COMMON
// =====================================================================

public sealed class Ditty : BardCard
{
    public Ditty() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(5, 1, (card, _) => Song.VersesOf(card), upgrade: 3);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class RallyCry : BardCard
{
    public RallyCry() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllAllies) { WithBlock(5, 2); WithPower<VigorPower>(3, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await GuardAllies(play, DynamicVars.Block.BaseValue);
        await BuffAllies<VigorPower>(ctx);
    }
}

public sealed class QuickStep : BardCard
{
    public QuickStep() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { WithCards(1); WithBlock(3, 2); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await DrawCards(ctx); }
}

public sealed class BassLine : BardCard
{
    public BassLine() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, 4); WithVar("Extra", 2); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await AddVerses(ctx, V("Extra")); }
}

public sealed class SharpNote : BardCard
{
    public SharpNote() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 3); WithVar("Extra", 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await AddVerses(ctx, V("Extra")); }
}

public sealed class Lilt : BardCard
{
    public Lilt() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { WithBlock(6, 3); WithCards(1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await DrawCards(ctx); }
}

public sealed class TauntingTune : BardCard
{
    public TauntingTune() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<VulnerablePower>(2, 1); WithVar("Extra", 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Debuff<VulnerablePower>(ctx, play); await AddVerses(ctx, V("Extra")); }
}

public sealed class ChorusLine : BardCard
{
    public ChorusLine() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(5, 3); WithVar("Extra", 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await AddVerses(ctx, V("Extra")); }
}

public sealed class SecondVerse : BardCard
{
    public SecondVerse() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Extra", 2, 1); WithCards(1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await AddVerses(ctx, V("Extra")); await DrawCards(ctx); }
}

public sealed class Strum : BardCard
{
    public Strum() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { WithDamage(3, 1); WithVar("Repeat", 3); WithBlock(2, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play, V("Repeat")); await Guard(play); }
}

public sealed class Busking : BardCard
{
    public Busking() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 2); WithVar("ExtraBlock", 4, 1); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Guard(play);
        if (Singing) await GainBlock(V("ExtraBlock"), play);
    }
}

/// <summary>If no Song is playing, start Battle Hymn. Otherwise add 3 Verses.</summary>
public sealed class WarmUp : BardCard
{
    public WarmUp() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("Extra", 3, 1); WithTip(typeof(BattleHymnPower)); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Singing) await AddVerses(ctx, V("Extra"));
        else await SongCmd.Start<BattleHymnPower>(ctx, this, V("Extra"));
    }
}

// =====================================================================
//  UNCOMMON — Songs and powers
// =====================================================================

public sealed class WarDrums : SongCard<WarDrumsPower>
{
    public WarDrums() : base(1, CardRarity.Uncommon) => Song(1, 2);
}
public sealed class SeaShanty : SongCard<SeaShantyPower>
{
    public SeaShanty() : base(1, CardRarity.Uncommon) => Song(1, 2);
}
public sealed class Dirge : SongCard<DirgePower>
{
    public Dirge() : base(1, CardRarity.Uncommon) => Song(1, 2);
}

public sealed class Improvise : BardPowerCard<ImprovisePower>
{
    // Energy var only for the card text: energyIcons() needs an energy var, not a power var.
    public Improvise() : base(1, CardRarity.Uncommon, 1, 0, -1) => WithEnergy(1);
}
public sealed class StagePresence() : BardPowerCard<StagePresencePower>(1, CardRarity.Uncommon, 2, 1);
public sealed class Tempo() : BardPowerCard<TempoPower>(1, CardRarity.Uncommon, 1, 1);
public sealed class PerfectPitch() : BardPowerCard<PerfectPitchPower>(1, CardRarity.Uncommon, 2, 1);
public sealed class Accompaniment : BardPowerCard<AccompanimentPower>
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public Accompaniment() : base(1, CardRarity.Uncommon, 1, 0, -1) { }
}

// =====================================================================
//  UNCOMMON — skills and attacks
// =====================================================================

public sealed class VerseStrike : BardCard
{
    public VerseStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(3, 1, (card, _) => Song.VersesOf(card), upgrade: 3);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class BalladShield : BardCard
{
    public BalladShield() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(3, 1, (card, _) => Song.VersesOf(card), upgrade: 3);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

/// <summary>Your Song's Finale triggers now, without ending the Song.</summary>
public sealed class Medley : BardCard
{
    public Medley() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await SongCmd.Finale(ctx, Owner.Creature, 1);
}

public sealed class CrowdPleaser : BardCard
{
    public CrowdPleaser() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { WithBlock(10, 3); WithCards(1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await GuardAllies(play, DynamicVars.Block.BaseValue); await DrawCards(ctx); }
}

public sealed class LuteBash : BardCard
{
    public LuteBash() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(9, 3); WithPower<WeakPower>(1, 1); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        if (Singing) await Debuff<WeakPower>(ctx, play);
    }
}

public sealed class SoothingMelody : BardCard
{
    public SoothingMelody() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithHeal(3, 2); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var a in Allies) await CreatureCmd.Heal(a, DynamicVars.Heal.BaseValue);
    }
}

public sealed class RestNote : BardCard
{
    public RestNote() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(6, 3); WithKeywords(CardKeyword.Retain);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class HighNote : BardCard
{
    public HighNote() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(16, 10, (card, _) => Song.VersesOf(card) >= 10 ? 1 : 0, upgrade: 5);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class DrumRoll : BardCard
{
    public DrumRoll() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy) { WithDamage(2, 1); WithVar("Repeat", 6); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play, V("Repeat"));
}

/// <summary>Co-op only. Another player gains 3 Vigor and 5 Block.</summary>
public sealed class Inspire : BardCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public Inspire() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { WithPower<VigorPower>(3, 1); WithBlock(5, 2); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target == null) return;
        await CommonActions.Apply<VigorPower>(ctx, play.Target, this);
        await CreatureCmd.GainBlock(play.Target, DynamicVars.Block, play);
    }
}

public sealed class RousingSpeech : BardCard
{
    public RousingSpeech() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, 1); WithVar("Extra", 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await DrawCards(ctx); await AddVerses(ctx, V("Extra")); }
}

public sealed class Feedback : BardCard
{
    public Feedback() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { WithDamage(7, 3); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class OffBeat : BardCard
{
    public OffBeat() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { WithPower<WeakPower>(2, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await DebuffAll<WeakPower>(ctx);
}

/// <summary>Gain 2 Block per Verse, then your Song drops to 1 Verse.</summary>
public sealed class Diminuendo : BardCard
{
    public Diminuendo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("PerVerse", 2, 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var v = Verses;
        if (v == 0) return;
        await GainBlock(v * V("PerVerse"), play);
        await SongCmd.SetVerses(ctx, Owner.Creature, 1);
    }
}

public sealed class VocalWarmup : BardCard
{
    public VocalWarmup() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Extra", 3, 2); WithKeywords(CardKeyword.Exhaust); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await AddVerses(ctx, V("Extra"));
}

public sealed class Tambourine : BardCard
{
    public Tambourine() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { WithDamage(5, 2); WithVar("Repeat", 2); WithBlock(3, 1); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play, V("Repeat")); await Guard(play); }
}

public sealed class Cacophony : BardCard
{
    public Cacophony() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1); WithPower<VulnerablePower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await DebuffAll<WeakPower>(ctx); await DebuffAll<VulnerablePower>(ctx); }
}

public sealed class SongAndDance : BardCard
{
    public SongAndDance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(5, 2); WithCards(1); WithVar("Extra", 1, 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await DrawCards(ctx); await AddVerses(ctx, V("Extra")); }
}

public sealed class Bow : BardCard
{
    public Bow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { WithBlock(9, 3); WithKeywords(CardKeyword.Retain); }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Guard(play);
}

public sealed class Counterpoint : BardCard
{
    public Counterpoint() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3); WithBlock(6, 2); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        if (Singing) await Guard(play);
    }
}

public sealed class Serenade : BardCard
{
    public Serenade() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(15, 5); WithVar("Extra", 2); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Guard(play); await AddVerses(ctx, V("Extra")); }
}

// =====================================================================
//  RARE — Songs and powers
// =====================================================================

public sealed class OdeToJoy : SongCard<OdeToJoyPower>
{
    public OdeToJoy() : base(2, CardRarity.Rare) => Song(1, 2);
}
public sealed class Anthem : SongCard<AnthemPower>
{
    public Anthem() : base(2, CardRarity.Rare) => Song(1, 2);
}
public sealed class SirenSong : SongCard<SirenSongPower>
{
    public SirenSong() : base(2, CardRarity.Rare) => Song(1, 2);
}

public sealed class Virtuoso() : BardPowerCard<VirtuosoPower>(3, CardRarity.Rare, 1, 0, -1);
public sealed class Maestro() : BardPowerCard<MaestroPower>(2, CardRarity.Rare, 1, 0, -1);
public sealed class LegendaryBallad() : BardPowerCard<LegendaryBalladPower>(1, CardRarity.Rare, 10, 5);
public sealed class BardicInspiration() : BardPowerCard<BardicInspirationPower>(2, CardRarity.Rare, 1, 0, -1);
public sealed class GrandStage() : BardPowerCard<GrandStagePower>(1, CardRarity.Rare, 8, 4);
public sealed class Muse() : BardPowerCard<MusePower>(1, CardRarity.Rare, 1, 0, -1);

// =====================================================================
//  RARE — skills and attacks
// =====================================================================

public sealed class Fortissimo : BardCard
{
    public Fortissimo() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithCalculatedDamage(0, 2, (card, _) => Song.VersesOf(card), bonusUpgrade: 1);
        WithKeywords(CardKeyword.Exhaust); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

public sealed class SwanSong : BardCard
{
    public SwanSong() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Times", 3, 1); WithKeywords(CardKeyword.Exhaust); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await SongCmd.End(ctx, Owner.Creature, V("Times"));
}

public sealed class Showstopper : BardCard
{
    public Showstopper() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(24, 2, (card, _) => Song.VersesOf(card), upgrade: 8);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Hit(ctx, play);
}

/// <summary>Your Song reaches its maximum Verses. Exhaust.</summary>
public sealed class MagnumOpus : BardCard
{
    public MagnumOpus() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await SongCmd.SetVerses(ctx, Owner.Creature, SongCmd.MaxVerses(Owner.Creature));
}

/// <summary>Your Song's Finale triggers, then its Refrain. Exhaust.</summary>
public sealed class CurtainCall : BardCard
{
    public CurtainCall() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await SongCmd.Finale(ctx, Owner.Creature, 1);
        var song = SongCmd.Current(Owner.Creature);
        if (song != null) await song.Refrain(ctx);
    }
}

public sealed class PowerBallad : BardCard
{
    public PowerBallad() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(11, 4); WithVar("Repeat", 2); WithVar("Extra", 2); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play, V("Repeat")); await AddVerses(ctx, V("Extra")); }
}

/// <summary>Double your Song's Verses. Exhaust.</summary>
public sealed class MezzoForte : BardCard
{
    public MezzoForte() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await AddVerses(ctx, Verses);
}

public sealed class HarmonicShield : BardCard
{
    public HarmonicShield() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithCalculatedBlock(4, 1, (card, _) => Song.VersesOf(card), upgrade: 4);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var amount = DynamicVars.CalculatedBlock.Calculate(Owner.Creature);
        await GuardAllies(play, amount);
    }
}

public sealed class Lifesong : BardCard
{
    public Lifesong() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithHeal(6, 3); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var a in Allies) await CreatureCmd.Heal(a, DynamicVars.Heal.BaseValue);
    }
}

/// <summary>Co-op only. Another player gains 1 Energy and draws 2 cards. Exhaust.</summary>
public sealed class GrandDuet : BardCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public GrandDuet() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
        WithEnergy(1); WithCards(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var p = play.Target?.Player;
        if (p == null) return;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, p);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, p);
    }
}

public sealed class ThunderousApplause : BardCard
{
    public ThunderousApplause() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(14, 4); WithPower<WeakPower>(1, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await DebuffAll<WeakPower>(ctx); }
}

public sealed class BattleCry : BardCard
{
    public BattleCry() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithPower<StrengthPower>(2, 1); WithKeywords(CardKeyword.Exhaust);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await BuffAllies<StrengthPower>(ctx);
}

public sealed class SonicBoom : BardCard
{
    public SonicBoom() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 4); WithPower<VulnerablePower>(2, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await Hit(ctx, play); await Debuff<VulnerablePower>(ctx, play); }
}

/// <summary>If your Song has 10 or more Verses, gain 2 Energy and draw 2 cards.</summary>
public sealed class Coda : BardCard
{
    public Coda() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithEnergy(2); WithCards(2, 1); WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Verses < 10) return;
        await Energy(DynamicVars.Energy.BaseValue);
        await DrawCards(ctx);
    }
}

/// <summary>Innate. Start Battle Hymn with 5 Verses. Exhaust.</summary>
public sealed class OpeningAct : BardCard
{
    public OpeningAct() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Verses", 5, 3); WithKeywords(CardKeyword.Innate, CardKeyword.Exhaust); WithTip(typeof(BattleHymnPower));
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await SongCmd.Start<BattleHymnPower>(ctx, this, V("Verses"));
}
