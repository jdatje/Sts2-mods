using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Character;
using CardSharp.CardSharpCode.Extensions;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CardSharp.CardSharpCode.Relics;

/// <summary>
/// The Card Sharp's starter relics (Marked Deck and its Ancient version Stacked Deck).
/// The Pot: each Showdown deals 2 more damage than the last, this combat, up to a cap.
/// They also keep the Table overlay shown, placed and cleared.
/// </summary>
public abstract class PotRelic : CardSharp.CardSharpCode.Visuals.AnimatedStarterRelic, IShowdownPot
{
    public const int PotStep = 2;
    private int _showdowns;

    // ------------------------------------------------------------ run progress (saved with the run)

    /// <summary>Last known progress per player, so the Ancient upgrade (Touch of Orobas) keeps it.</summary>
    private static readonly Dictionary<ulong, (int a, int b, int c, int rules)> LastKnown = [];

    /// <summary>Times each hand was scored, packed 7 bits per hand, 3 hands per int.</summary>
    [SavedProperty] public int HandScoresA { get; set; }
    [SavedProperty] public int HandScoresB { get; set; }
    [SavedProperty] public int HandScoresC { get; set; }
    /// <summary>House Rules picked, one bit per rule.</summary>
    [SavedProperty] public int HouseRules { get; set; }

    private const int Bits = 7, Mask = (1 << Bits) - 1;

    public int Scores(HandType hand)
    {
        var i = (int)hand;
        var word = (i / 3) switch { 0 => HandScoresA, 1 => HandScoresB, _ => HandScoresC };
        return (word >> (i % 3 * Bits)) & Mask;
    }

    private void SetScores(HandType hand, int value)
    {
        var i = (int)hand;
        value = Math.Clamp(value, 0, Mask);
        var shift = i % 3 * Bits;
        int Put(int word) => (word & ~(Mask << shift)) | (value << shift);
        switch (i / 3)
        {
            case 0: HandScoresA = Put(HandScoresA); break;
            case 1: HandScoresB = Put(HandScoresB); break;
            default: HandScoresC = Put(HandScoresC); break;
        }
    }

    public int HandLevel(HandType hand) => HandLevels.LevelFor(hand, Scores(hand));

    /// <summary>Called after every Showdown with the hand that was scored.</summary>
    public void AddScore(HandType hand)
    {
        if (HandLevels.ScoresPerLevel(hand) <= 0 || HandLevel(hand) >= HandLevels.MaxLevel) return;
        var before = HandLevel(hand);
        SetScores(hand, Scores(hand) + 1);
        Remember();
        if (HandLevel(hand) > before)
        {
            Flash();
            InvokeDisplayAmountChanged();
        }
    }

    public bool HasRule(HouseRule rule) => (HouseRules & (1 << (int)rule)) != 0;
    public IEnumerable<HouseRule> MissingRules => Enum.GetValues<HouseRule>().Where(r => !HasRule(r));
    public IEnumerable<HouseRule> OwnedRules => Enum.GetValues<HouseRule>().Where(HasRule);

    public void AddRule(HouseRule rule)
    {
        HouseRules |= 1 << (int)rule;
        Remember();
        InvokeDisplayAmountChanged();
    }

    /// <summary>The counter on the relic: the highest Hand Level.</summary>
    public override bool ShowCounter => true;
    public override int DisplayAmount => Enum.GetValues<HandType>().Select(HandLevel).Max();

    private void Remember()
    {
        if (Owner != null) LastKnown[Owner.NetId] = (HandScoresA, HandScoresB, HandScoresC, HouseRules);
    }

    /// <summary>When this relic replaces Marked Deck (Ancient upgrade), carry over the progress.</summary>
    public override Task AfterObtained()
    {
        var other = Owner.Relics.OfType<PotRelic>().FirstOrDefault(r => r != this);
        if (other != null)
        {
            (HandScoresA, HandScoresB, HandScoresC, HouseRules) =
                (other.HandScoresA, other.HandScoresB, other.HandScoresC, other.HouseRules);
        }
        else if (HandScoresA == 0 && HandScoresB == 0 && HandScoresC == 0 && HouseRules == 0
                 && LastKnown.TryGetValue(Owner.NetId, out var k))
        {
            (HandScoresA, HandScoresB, HandScoresC, HouseRules) = k;
        }
        Remember();
        return Task.CompletedTask;
    }

    /// <summary>"Deal Me In" at rest sites: pick 1 of 3 random House Rules.</summary>
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || !MissingRules.Any()) return false;
        options.Add(new DealMeInRestSiteOption(player));
        return true;
    }

    /// <summary>Quick Deal: true until the first Showdown of this combat.</summary>
    public bool FirstShowdownPending => _showdowns == 0;

    protected abstract int PotCap { get; }


    public int CurrentPot => Math.Min(PotCap, PotStep * _showdowns);

    public int TakePot()
    {
        var pot = CurrentPot;
        _showdowns++;
        if (pot > 0) Flash();
        return pot;
    }

    /// <summary>Shows the (empty) Table panel from the first turn on, and keeps it placed above her.</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner) TableOverlay.Refresh(Owner.Creature);
    }

    /// <summary>Remove the Table panel as soon as the fight is won, so it doesn't linger on the map.</summary>
    public override async Task AfterCombatVictoryEarly(CombatRoom room)
    {
        await base.AfterCombatVictoryEarly(room);
        TableOverlay.Hide(Owner.Creature);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        TableCmd.Clear(Owner.Creature);
        TableOverlay.Hide(Owner.Creature);
    }

    public override Task BeforeCombatStart()
    {
        _showdowns = 0;
        Remember();
        TableCmd.Clear(Owner.Creature);
        return Task.CompletedTask;
    }
}

/// <summary>Starter relic. The Pot grows +2 per Showdown, up to +7.</summary>
[Pool(typeof(CardSharpRelicPool))]
public sealed class MarkedDeck : PotRelic
{
    public const int Cap = 7;
    protected override int PotCap => Cap;
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<StackedDeck>();
}

/// <summary>Ancient (upgraded starter). The Pot grows +2 per Showdown, up to +10.</summary>
[Pool(typeof(SharedRelicPool))]
public sealed class StackedDeck : PotRelic
{
    public const int Cap = 10;
    protected override int PotCap => Cap;
    public override RelicRarity Rarity => RelicRarity.Ancient;
}

/// <summary>Rest site option: pick 1 of 3 random House Rules the Card Sharp doesn't have yet.</summary>
public sealed class DealMeInRestSiteOption(Player owner) : CustomRestSiteOption(owner)
{
    public override string OptionId => "CARDSHARP_DEAL_ME_IN";
    public override string? CustomIconPath => MainFile.ResPath + "/images/charui/rest_deal_cardsharp.png";

    public override async Task<bool> OnSelect()
    {
        var relic = HandLevels.Relic(Owner);
        if (relic == null) return false;
        var missing = relic.MissingRules.ToList();
        if (missing.Count == 0) return false;

        var rng = Owner.PlayerRng.Rewards;
        var offer = new List<HouseRule>();
        while (offer.Count < 3 && missing.Count > 0)
        {
            var pick = missing[rng.NextInt(0, missing.Count)];
            missing.Remove(pick);
            offer.Add(pick);
        }

        var cards = offer.Select(r => CardSharp.CardSharpCode.Cards.HouseRuleCard.Create(r, Owner)).ToList();
        var chosen = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, Owner, canSkip: false);
        if (chosen is not CardSharp.CardSharpCode.Cards.HouseRuleCard rc) return false;
        relic.AddRule(rc.Rule);
        return true;
    }
}
