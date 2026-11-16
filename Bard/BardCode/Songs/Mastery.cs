using Bard.BardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Songs;

/// <summary>Stanzas: permanent upgrades for one Song (max 2 per Song), picked after a fight where it reached 10 Verses.</summary>
public enum Stanza { OpeningLine, Echo, Resonant, BrightRefrain, GrandFinale, Lingering }

/// <summary>Harmonies: bonus effects for starting Song B while Song A is playing. Learned the first time.</summary>
public enum Harmony { Crescendo, Rally, CallToArms, SirensTrance, LastRites, DanceParty, Homeward, Banners }

/// <summary>Run-long Song progress: Mastery (per-Song Levels), Stanzas and Harmonies. State lives on the lute relics.</summary>
public static class SongBook
{
    public const int MaxLevel = 5, XpPerLevel = 5, MinVersesForXp = 5, MinVersesForStanza = 10, MaxStanzas = 2;
    public const int OpeningLineVerses = 3, ResonantVerses = 3;
    public const decimal FinalePerLevel = 0.10m, GrandFinaleBonus = 0.15m;

    public static readonly Type[] Songs =
    [
        typeof(BattleHymnPower), typeof(BalladOfValorPower), typeof(LullabyPower), typeof(JigPower),
        typeof(RequiemPower), typeof(WarDrumsPower), typeof(SeaShantyPower), typeof(DirgePower),
        typeof(OdeToJoyPower), typeof(AnthemPower), typeof(SirenSongPower),
    ];

    public static readonly string[] Names =
    [
        "Battle Hymn", "Ballad of Valor", "Lullaby", "Jig", "Requiem", "War Drums",
        "Sea Shanty", "Dirge", "Ode to Joy", "Anthem", "Siren Song",
    ];

    public static int Index(Type t) => Array.IndexOf(Songs, t);

    public static LuteRelic? Lute(Creature bard) => bard.Player?.Relics.OfType<LuteRelic>().FirstOrDefault();

    public static int LevelFor(int xp) => Math.Min(MaxLevel, xp / XpPerLevel);

    public static string StanzaName(Stanza s) => s switch
    {
        Stanza.OpeningLine => "Opening Line",
        Stanza.Echo => "Echo",
        Stanza.Resonant => "Resonant",
        Stanza.BrightRefrain => "Bright Refrain",
        Stanza.GrandFinale => "Grand Finale",
        _ => "Lingering",
    };

    public static readonly (Type from, Type to, Harmony harmony)[] Harmonies =
    [
        (typeof(BattleHymnPower), typeof(RequiemPower), Harmony.Crescendo),
        (typeof(BalladOfValorPower), typeof(BattleHymnPower), Harmony.Rally),
        (typeof(WarDrumsPower), typeof(BattleHymnPower), Harmony.CallToArms),
        (typeof(LullabyPower), typeof(SirenSongPower), Harmony.SirensTrance),
        (typeof(DirgePower), typeof(RequiemPower), Harmony.LastRites),
        (typeof(JigPower), typeof(OdeToJoyPower), Harmony.DanceParty),
        (typeof(SeaShantyPower), typeof(BalladOfValorPower), Harmony.Homeward),
        (typeof(AnthemPower), typeof(WarDrumsPower), Harmony.Banners),
    ];

    public static string HarmonyName(Harmony h) => h switch
    {
        Harmony.Crescendo => "Crescendo",
        Harmony.Rally => "Rally",
        Harmony.CallToArms => "Call to Arms",
        Harmony.SirensTrance => "Siren's Trance",
        Harmony.LastRites => "Last Rites",
        Harmony.DanceParty => "Dance Party",
        Harmony.Homeward => "Homeward",
        _ => "Banners",
    };

    public static string HarmonyPair(Harmony h)
    {
        var e = Harmonies.First(x => x.harmony == h);
        return $"{Names[Index(e.from)]} → {Names[Index(e.to)]}";
    }

    /// <summary>
    /// Called after Song <paramref name="song"/> started while <paramref name="from"/> (with <paramref name="oldVerses"/>) was playing.
    /// Learns the Harmony the first time and applies it.
    /// </summary>
    public static async Task TryHarmony(PlayerChoiceContext ctx, Creature bard, Type from, SongPower song, int oldVerses)
    {
        var lute = Lute(bard);
        if (lute == null) return;
        var match = Harmonies.Where(x => x.from == from && x.to == song.GetType()).Select(x => (Harmony?)x.harmony).FirstOrDefault();
        if (match is not { } h) return;
        lute.Learn(h);
        lute.Flash();

        var allies = bard.CombatState?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? [];
        var enemies = bard.CombatState?.HittableEnemies.ToList() ?? [];
        switch (h)
        {
            case Harmony.Crescendo:
                await SongCmd.AddVerses(ctx, bard, oldVerses / 2);
                break;
            case Harmony.Rally:
                await SongCmd.AddVerses(ctx, bard, oldVerses);
                break;
            case Harmony.CallToArms:
                if (oldVerses / 2 > 0) await PowerCmd.Apply<VigorPower>(ctx, allies, oldVerses / 2, bard, null);
                break;
            case Harmony.SirensTrance:
                if (enemies.Count > 0) await PowerCmd.Apply<VulnerablePower>(ctx, enemies, 2, bard, null);
                break;
            case Harmony.LastRites:
                foreach (var e in enemies)
                {
                    var doom = (int)e.Powers.OfType<DoomPower>().Sum(p => p.Amount) / 2;
                    if (doom > 0 && e.IsAlive)
                        await CreatureCmd.Damage(ctx, [e], doom, ValueProp.Unblockable | ValueProp.Unpowered, bard);
                }
                break;
            case Harmony.DanceParty:
                foreach (var a in allies) if (a.Player != null) await CardPileCmd.Draw(ctx, 2, a.Player);
                break;
            case Harmony.Homeward:
                if (oldVerses > 0) foreach (var a in allies) await CreatureCmd.GainBlock(a, oldVerses, ValueProp.Unpowered, null);
                break;
            case Harmony.Banners:
                await PowerCmd.Apply<PlatingPower>(ctx, allies, 2, bard, null);
                break;
        }
    }
}
