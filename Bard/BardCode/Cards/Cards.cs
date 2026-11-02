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

/// <summary>Base for Song cards: Power cards that start a Song with some Verses (upgrade = more Verses).</summary>
public abstract class SongCard<T>(int cost, CardRarity rarity) : BardCard(cost, CardType.Power, rarity, TargetType.Self), ISongCard
    where T : SongPower
{
    protected void Song(int verses, int upgrade)
    {
        WithVar("Verses", verses, upgrade);
        WithKeywords(BardKeywords.Song);
        WithTip(typeof(T));
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await SongCmd.Start<T>(ctx, this, DynamicVars["Verses"].IntValue);
}

// ================= Basic =================

public sealed class StrikeBard : BardCard
{
    public StrikeBard() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
        WithTags(CardTag.Strike);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

public sealed class DefendBard : BardCard
{
    public DefendBard() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(5, 3);
        WithTags(CardTag.Defend);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardBlock(this, play);
}

public sealed class BattleHymn : SongCard<BattleHymnPower>
{
    public BattleHymn() : base(1, CardRarity.Basic) => Song(1, 2);
}

/// <summary>Gain 4 Block. Add 1 extra Verse.</summary>
public sealed class Hum : BardCard
{
    public Hum() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(4, 2);
        WithVar("Extra", 1, 1);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await SongCmd.AddVerses(ctx, Owner.Creature, DynamicVars["Extra"].IntValue);
    }
}

// ================= Common =================

/// <summary>Deal 7 damage. Add 1 extra Verse.</summary>
public sealed class PowerChord : BardCard
{
    public PowerChord() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithVar("Extra", 1);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await SongCmd.AddVerses(ctx, Owner.Creature, DynamicVars["Extra"].IntValue);
    }
}

/// <summary>Deal 5 damage twice.</summary>
public sealed class Drumbeat : BardCard
{
    public Drumbeat() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithVar("Repeat", 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play, DynamicVars["Repeat"].IntValue).Execute(ctx);
}

/// <summary>Deal 18 damage.</summary>
public sealed class Falsetto : BardCard
{
    public Falsetto() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(18, 5);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.CardAttack(this, play).Execute(ctx);
}

/// <summary>ALL players gain 5 Block.</summary>
public sealed class Harmony : BardCard
{
    public Harmony() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllAllies)
    {
        WithBlock(5, 2);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (CombatState == null) return;
        foreach (var ally in CombatState.PlayerCreatures.Where(c => c.IsAlive))
            await CreatureCmd.GainBlock(ally, DynamicVars.Block, play);
    }
}

/// <summary>Gain 7 Block. Add 1 extra Verse.</summary>
public sealed class Cadence : BardCard
{
    public Cadence() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(7, 3);
        WithVar("Extra", 1);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await SongCmd.AddVerses(ctx, Owner.Creature, DynamicVars["Extra"].IntValue);
    }
}

/// <summary>Draw 1 card. Add 1 extra Verse.</summary>
public sealed class TuneUp : BardCard
{
    public TuneUp() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1);
        WithVar("Extra", 1);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
        await SongCmd.AddVerses(ctx, Owner.Creature, DynamicVars["Extra"].IntValue);
    }
}

/// <summary>Apply 2 Weak.</summary>
public sealed class Discord : BardCard
{
    public Discord() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<WeakPower>(2, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null) await CommonActions.Apply<WeakPower>(ctx, play.Target, this);
    }
}

public sealed class Jig : SongCard<JigPower>
{
    public Jig() : base(1, CardRarity.Common) => Song(1, 2);
}

// ================= Uncommon =================

public sealed class BalladOfValor : SongCard<BalladOfValorPower>
{
    public BalladOfValor() : base(1, CardRarity.Uncommon) => Song(1, 2);
}

public sealed class Lullaby : SongCard<LullabyPower>
{
    public Lullaby() : base(1, CardRarity.Uncommon) => Song(1, 2);
}

/// <summary>Add 4 Verses.</summary>
public sealed class Crescendo : BardCard
{
    public Crescendo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Extra", 4, 2);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await SongCmd.AddVerses(ctx, Owner.Creature, DynamicVars["Extra"].IntValue);
}

/// <summary>Your Song's Refrain triggers now. Upgrade: costs 0.</summary>
public sealed class Encore : BardCard
{
    public Encore() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var song = SongCmd.Current(Owner.Creature);
        if (song != null) await song.Refrain(ctx);
    }
}

/// <summary>End your Song. Its Finale triggers twice. Exhaust.</summary>
public sealed class GrandFinale : BardCard
{
    public GrandFinale() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1);
        WithTip(BardKeywords.Song);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await SongCmd.End(ctx, Owner.Creature, 2);
}

/// <summary>Power. At the start of your turn, add 1 Verse.</summary>
public sealed class Metronome : BardCard
{
    public Metronome() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<MetronomePower>(1);
        WithCostUpgradeBy(-1);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CommonActions.ApplySelf<MetronomePower>(ctx, this);
}

/// <summary>Co-op only. Give another player 8 Block. Gain 4 Block.</summary>
public sealed class Duet : BardCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public Duet() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithBlock(8, 3);
        WithVar("SelfBlock", 4, 1);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null) await CreatureCmd.GainBlock(play.Target, DynamicVars.Block, play);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["SelfBlock"].BaseValue, ValueProp.Move, play);
    }
}

// ================= Rare =================

public sealed class Requiem : SongCard<RequiemPower>
{
    public Requiem() : base(2, CardRarity.Rare) => Song(1, 2);
}

/// <summary>ALL players gain 3 Block for each Verse in your Song. Exhaust.</summary>
public sealed class StandingOvation : BardCard
{
    public StandingOvation() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
        WithVar("PerVerse", 3, 1);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(BardKeywords.Verse);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var song = SongCmd.Current(Owner.Creature);
        if (song == null || CombatState == null) return;
        var block = song.Verses * DynamicVars["PerVerse"].BaseValue;
        foreach (var ally in CombatState.PlayerCreatures.Where(c => c.IsAlive))
            await CreatureCmd.GainBlock(ally, block, ValueProp.Move, play);
    }
}
