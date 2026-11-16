using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Bard.BardCode.Relics;

/// <summary>
/// Base for the Worn Lute and Master Lute. Besides the first-Song bonus, they hold the Bard's run progress:
/// per-Song Mastery XP, Stanzas, learned Harmonies, and Stanza picks still waiting to be offered.
/// </summary>
public abstract class LuteRelic : Bard.BardCode.Visuals.AnimatedStarterRelic, IFirstSongBonus
{
    private static readonly Dictionary<ulong, (int, int, int, int, int, int, int)> LastKnown = [];

    // 5 bits of XP per Song, 6 Songs per int.
    [SavedProperty] public int SongXpA { get; set; }
    [SavedProperty] public int SongXpB { get; set; }
    // 6 Stanza bits per Song, 4 Songs per int.
    [SavedProperty] public int SongStanzasA { get; set; }
    [SavedProperty] public int SongStanzasB { get; set; }
    [SavedProperty] public int SongStanzasC { get; set; }
    [SavedProperty] public int Harmonies { get; set; }
    /// <summary>Bit per Song: a Stanza pick is waiting (offered when you enter the next non-combat room).</summary>
    [SavedProperty] public int PendingStanzas { get; set; }

    protected abstract int BonusVerses { get; }
    private bool _usedThisCombat;
    private readonly Dictionary<int, int> _maxVerses = [];

    public override bool ShowCounter => true;
    public override int DisplayAmount => Enumerable.Range(0, SongBook.Songs.Length).Max(Level);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("LuteProgress", "")];

    // ------------------------------------------------------------ packed state
    private int XpWord(int song) => song / 6 == 0 ? SongXpA : SongXpB;
    public int Xp(int song) => song < 0 ? 0 : (XpWord(song) >> (song % 6 * 5)) & 31;
    private void SetXp(int song, int value)
    {
        var shift = song % 6 * 5;
        var word = (XpWord(song) & ~(31 << shift)) | (Math.Clamp(value, 0, 31) << shift);
        if (song / 6 == 0) SongXpA = word; else SongXpB = word;
    }

    public int Level(int song) => SongBook.LevelFor(Xp(song));
    public int Level(Type songType) => Level(SongBook.Index(songType));

    private int StanzaWord(int song) => (song / 4) switch { 0 => SongStanzasA, 1 => SongStanzasB, _ => SongStanzasC };
    public int StanzaBits(int song) => song < 0 ? 0 : (StanzaWord(song) >> (song % 4 * 6)) & 63;
    public int StanzaBits(Type songType) => StanzaBits(SongBook.Index(songType));
    public bool HasStanza(int song, Stanza s) => (StanzaBits(song) & (1 << (int)s)) != 0;
    public int StanzaCount(int song) => Enum.GetValues<Stanza>().Count(s => HasStanza(song, s));

    public void AddStanza(int song, Stanza s)
    {
        var shift = song % 4 * 6;
        var word = StanzaWord(song) | (1 << ((int)s + shift));
        switch (song / 4) { case 0: SongStanzasA = word; break; case 1: SongStanzasB = word; break; default: SongStanzasC = word; break; }
        Changed();
    }

    public bool Knows(Harmony h) => (Harmonies & (1 << (int)h)) != 0;
    public void Learn(Harmony h)
    {
        if (Knows(h)) return;
        Harmonies |= 1 << (int)h;
        Changed();
    }

    /// <summary>Extra max Verses for this Song: +1 per Mastery Level, +3 with Resonant.</summary>
    public int ExtraMaxVerses(Type songType)
    {
        var i = SongBook.Index(songType);
        if (i < 0) return 0;
        return Level(i) + (HasStanza(i, Stanza.Resonant) ? SongBook.ResonantVerses : 0);
    }

    // ------------------------------------------------------------ tracking
    /// <summary>Remembers the most Verses each Song reached this combat.</summary>
    public void Track(Type songType, int verses)
    {
        var i = SongBook.Index(songType);
        if (i < 0) return;
        _maxVerses[i] = Math.Max(verses, _maxVerses.GetValueOrDefault(i));
    }

    private void Changed()
    {
        Remember();
        if (DynamicVars.TryGetValue("LuteProgress", out var v) && v is StringVar s) s.StringValue = ProgressText();
        InvokeDisplayAmountChanged();
    }

    public string ProgressText()
    {
        var lines = new List<string>();
        for (var i = 0; i < SongBook.Songs.Length; i++)
        {
            var stanzas = Enum.GetValues<Stanza>().Where(s => HasStanza(i, s)).Select(SongBook.StanzaName).ToList();
            if (Xp(i) == 0 && stanzas.Count == 0) continue;
            var line = $"{SongBook.Names[i]} [gold]Lv {Level(i)}[/gold]";
            if (Level(i) < SongBook.MaxLevel) line += $" ({Xp(i) % SongBook.XpPerLevel}/{SongBook.XpPerLevel})";
            if (stanzas.Count > 0) line += " · " + string.Join(", ", stanzas);
            lines.Add(line);
        }
        var known = Enum.GetValues<Harmony>().Where(Knows).Select(h => $"{SongBook.HarmonyName(h)} ({SongBook.HarmonyPair(h)})").ToList();
        if (known.Count > 0) lines.Add("Harmonies: " + string.Join(", ", known));
        return lines.Count == 0 ? "" : "\n\n" + string.Join("\n", lines);
    }

    private void Remember()
    {
        if (Owner != null)
            LastKnown[Owner.NetId] = (SongXpA, SongXpB, SongStanzasA, SongStanzasB, SongStanzasC, Harmonies, PendingStanzas);
    }

    private bool IsEmpty => SongXpA == 0 && SongXpB == 0 && SongStanzasA == 0 && SongStanzasB == 0 && SongStanzasC == 0 && Harmonies == 0;

    /// <summary>When this relic replaces the other lute (Ancient upgrade), carry over its progress.</summary>
    public override Task AfterObtained()
    {
        var other = Owner.Relics.OfType<LuteRelic>().FirstOrDefault(r => r != this);
        if (other != null)
            (SongXpA, SongXpB, SongStanzasA, SongStanzasB, SongStanzasC, Harmonies, PendingStanzas) =
                (other.SongXpA, other.SongXpB, other.SongStanzasA, other.SongStanzasB, other.SongStanzasC, other.Harmonies, other.PendingStanzas);
        // Only the Ancient version falls back on the remembered copy: a new starter in a new run must start empty.
        else if (Rarity == MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Ancient && IsEmpty && LastKnown.TryGetValue(Owner.NetId, out var k))
            (SongXpA, SongXpB, SongStanzasA, SongStanzasB, SongStanzasC, Harmonies, PendingStanzas) = k;
        Changed();
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------ hooks
    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        _maxVerses.Clear();
        Changed();
        return Task.CompletedTask;
    }

    /// <summary>Called when a Song starts: the bonus Verses the first time each combat, otherwise 0.</summary>
    public int TryUse()
    {
        if (_usedThisCombat) return 0;
        _usedThisCombat = true;
        Flash();
        return BonusVerses;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        var current = SongCmd.Current(Owner.Creature);
        if (current != null) Track(current.GetType(), current.Verses);

        var levelled = false;
        foreach (var (song, max) in _maxVerses)
        {
            if (max >= SongBook.MinVersesForXp && Xp(song) < SongBook.MaxLevel * SongBook.XpPerLevel)
            {
                var before = Level(song);
                SetXp(song, Xp(song) + 1);
                levelled |= Level(song) != before;
            }
            if (max >= SongBook.MinVersesForStanza && StanzaCount(song) < SongBook.MaxStanzas)
                PendingStanzas |= 1 << song;
        }
        _maxVerses.Clear();
        if (levelled) Flash();
        Changed();
    }

    /// <summary>Offers waiting Stanza picks when you enter a room without a fight (rest site, shop, event, treasure).</summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room);
        Remember();
        if (room is CombatRoom || PendingStanzas == 0 || Owner == null) return;

        var rng = Owner.PlayerRng.Rewards;
        for (var song = 0; song < SongBook.Songs.Length; song++)
        {
            if ((PendingStanzas & (1 << song)) == 0) continue;
            PendingStanzas &= ~(1 << song);
            Remember();
            if (StanzaCount(song) >= SongBook.MaxStanzas) continue;

            var missing = Enum.GetValues<Stanza>().Where(s => !HasStanza(song, s)).ToList();
            var offer = new List<Stanza>();
            while (offer.Count < 2 && missing.Count > 0)
            {
                var pick = missing[rng.NextInt(0, missing.Count)];
                missing.Remove(pick);
                offer.Add(pick);
            }
            if (offer.Count == 0) continue;

            var cards = offer.Select(s => Bard.BardCode.Cards.StanzaCard.Create(s, song, Owner)).ToList();
            var chosen = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, Owner, canSkip: false);
            if (chosen is Bard.BardCode.Cards.StanzaCard sc) AddStanza(song, sc.Stanza);
        }
        Changed();
    }
}
