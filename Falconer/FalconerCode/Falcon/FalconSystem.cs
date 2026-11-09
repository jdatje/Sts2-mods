using Falconer.FalconerCode.Powers;
using Falconer.FalconerCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Falconer.FalconerCode.Falcon;

/// <summary>The jobs the falcon can be sent on.</summary>
public enum Errand { Hunt, Stoop, Scout, Fetch, Harry, Forage }

/// <summary>Powers and relics that react to the falcon leaving and coming back.</summary>
public interface IFalconListener
{
    Task AfterSend(PlayerChoiceContext ctx, Errand errand) => Task.CompletedTask;
    Task AfterReturn(PlayerChoiceContext ctx, Errand errand, int damageDealt) => Task.CompletedTask;
    Task AfterRecall(PlayerChoiceContext ctx) => Task.CompletedTask;
    /// <summary>Extra Errand strength in percent (Imprint, Molting Oil).</summary>
    int ErrandBonusPct => 0;
}

/// <summary>Traits picked at the rest site ("Fly"). Stored as a bit mask on the starter relic.</summary>
public enum Trait
{
    SharpTalons, TwinBrood, KeenEye, SwiftWings, Homing, Loyal,
    ThickPlumage, Magpie, TerrorOfTheSkies, Patient, Bonded, WideRange,
}

public static class FalconCmd
{
    // ---------------- Base numbers ----------------
    public const int HuntDamage = 10, StoopDamage = 24, ScoutDraw = 2, ScoutBlock = 4;
    public const int HarryDamage = 5, ForageHeal = 3, MagpieGold = 8;
    public const int MaxLevel = 10, PctPerLevel = 10;

    public static int Turns(Errand e) => e switch
    {
        Errand.Stoop or Errand.Harry => 2,
        Errand.Forage => 3,
        _ => 1,
    };

    // ---------------- Run state (on the starter relic) ----------------
    public static GloveRelic? Glove(Player? p) => p?.Relics.OfType<GloveRelic>().FirstOrDefault();
    public static int Level(Player? p) => Glove(p)?.Level ?? 1;
    public static bool Has(Player? p, Trait t) => Glove(p)?.HasTrait(t) == true;

    // ---------------- Falcon state ----------------
    public static AwayPower? Away(Creature falconer) => falconer.Powers.OfType<AwayPower>().FirstOrDefault();
    public static bool IsAway(Creature falconer) => Away(falconer) != null;

    /// <summary>"Perched:" bonuses. Partners makes both bonuses always count.</summary>
    public static bool Perched(Creature falconer) => !IsAway(falconer) || falconer.Powers.OfType<PartnersPower>().Any();
    public static bool Alone(Creature falconer) => IsAway(falconer) || falconer.Powers.OfType<PartnersPower>().Any();

    /// <summary>Strength multiplier in percent for an Errand at the given resolve percentage.</summary>
    public static int Strength(Creature falconer, int pct, int extraPct = 0)
    {
        var level = Math.Clamp(Level(falconer.Player), 1, MaxLevel);
        var bonus = PctPerLevel * (level - 1) + extraPct + Listeners(falconer).Sum(l => l.ErrandBonusPct);
        return pct * (100 + bonus) / 100;
    }

    public static int Scale(int value, int strengthPct) => Math.Max(0, value * strengthPct / 100);

    public static IEnumerable<IFalconListener> Listeners(Creature falconer) =>
        falconer.Powers.OfType<IFalconListener>()
            .Concat(falconer.Player?.Relics.OfType<IFalconListener>() ?? []).ToList();

    // ---------------- Commands ----------------

    /// <summary>Sends the falcon. A falcon that is already Away first resolves its Errand at 50%.</summary>
    public static async Task Send(PlayerChoiceContext ctx, Creature falconer, Errand errand,
        Creature? forCreature = null, Creature? harryTarget = null, int extraPct = 0, int flat = 0, bool chain = false)
    {
        var old = Away(falconer);
        if (old != null) await ReturnNow(ctx, falconer, 50);

        var turns = Turns(errand);
        var player = falconer.Player;
        if (turns > 1 && Has(player, Trait.SwiftWings)) turns--;
        var glove = Glove(player);
        if (glove != null && turns > 1 && glove.TakeSilverBells()) turns--;

        if (errand == Errand.Harry && harryTarget == null)
            harryTarget = RandomEnemy(falconer);

        await PowerCmd.Apply<AwayPower>(ctx, falconer, turns, falconer, null);
        var away = Away(falconer);
        if (away == null) return;
        away.Errand = errand;
        away.For = forCreature ?? falconer;
        away.HarryTarget = harryTarget;
        away.ExtraPct = extraPct;
        away.Flat = flat;
        away.Chain = chain;
        FalconOverlay.Leave(falconer);

        foreach (var l in Listeners(falconer)) await l.AfterSend(ctx, errand);
    }

    /// <summary>Recall: the current Errand resolves now (50%, or 100% with Homing) and the falcon is Perched.</summary>
    public static async Task Recall(PlayerChoiceContext ctx, Creature falconer, int? pct = null)
    {
        if (!IsAway(falconer)) return;
        var p = pct ?? (Has(falconer.Player, Trait.Homing) ? 100 : 50);
        await ReturnNow(ctx, falconer, p);
        foreach (var l in Listeners(falconer)) await l.AfterRecall(ctx);
        if (Has(falconer.Player, Trait.Bonded) && falconer.Player != null)
            await CardPileCmd.Draw(ctx, 1, falconer.Player);
    }

    /// <summary>The falcon comes back now and its Errand resolves at the given percentage.</summary>
    public static async Task ReturnNow(PlayerChoiceContext ctx, Creature falconer, int pct)
    {
        var away = Away(falconer);
        if (away == null) return;
        var (errand, forC, harry, extra, flat, chain) = (away.Errand, away.For, away.HarryTarget, away.ExtraPct, away.Flat, away.Chain);
        await PowerCmd.Remove(away);
        FalconOverlay.Land(falconer);
        await ResolveWithRepeats(ctx, falconer, errand, pct, forC, harry, extra, flat);
        if (chain) await Send(ctx, falconer, errand, forC, null, extra, flat, chain: false);
    }

    /// <summary>Resolves one Errand, plus the repeats from Sky Sovereign and Twin Brood. Fires AfterReturn once.</summary>
    public static async Task ResolveWithRepeats(PlayerChoiceContext ctx, Creature falconer, Errand errand, int pct,
        Creature? forC, Creature? harry, int extraPct = 0, int flat = 0)
    {
        var times = 1 + (falconer.Powers.OfType<SkySovereignPower>().Any() ? 1 : 0);
        var glove = Glove(falconer.Player);
        if (glove != null && glove.TakeTwinBrood()) times++;

        var dealt = 0;
        for (var i = 0; i < times; i++)
            dealt += await Resolve(ctx, falconer, errand, pct, forC, harry, extraPct, flat);

        glove?.CountErrand();
        if (Has(falconer.Player, Trait.ThickPlumage))
            await CreatureCmd.GainBlock(falconer, 3, ValueProp.Unpowered, null);
        foreach (var l in Listeners(falconer)) await l.AfterReturn(ctx, errand, dealt);
    }

    /// <summary>The effect of one Errand. Returns the damage it dealt.</summary>
    public static async Task<int> Resolve(PlayerChoiceContext ctx, Creature falconer, Errand errand, int pct,
        Creature? forC = null, Creature? harry = null, int extraPct = 0, int flat = 0)
    {
        var player = falconer.Player;
        var who = forC is { IsAlive: true } ? forC : falconer;
        var s = Strength(falconer, pct, extraPct);
        var dealt = 0;

        switch (errand)
        {
            case Errand.Hunt:
            {
                var dmg = Scale(HuntDamage + flat + (Has(player, Trait.SharpTalons) ? 4 : 0), s);
                var gyr = falconer.Powers.OfType<GyrfalconPower>().FirstOrDefault();
                var hits = 1;
                if (gyr != null) { dmg += Scale(gyr.Amount, s); hits = 2; }
                for (var i = 0; i < hits; i++)
                    dealt += await HitEnemy(ctx, falconer, MarkedOrRandom(falconer), dmg);
                break;
            }
            case Errand.Stoop:
            {
                var dmg = Scale(StoopDamage + flat, s);
                if (Has(player, Trait.WideRange))
                {
                    foreach (var e in Enemies(falconer).ToList())
                        dealt += await HitEnemy(ctx, falconer, e, dmg * 60 / 100);
                }
                else
                {
                    var target = Enemies(falconer).OrderByDescending(e => e.CurrentHp).FirstOrDefault();
                    dealt += await HitEnemy(ctx, falconer, target, dmg);
                }
                break;
            }
            case Errand.Scout:
            {
                var draw = ScoutDraw + flat + (Has(player, Trait.KeenEye) ? 1 : 0) + LevelSteps(player);
                draw = Math.Max(1, draw * pct / 100);
                var block = Scale(ScoutBlock + LevelSteps(player), s);
                if (block > 0) await CreatureCmd.GainBlock(who, block, ValueProp.Unpowered, null);
                if (who.Player != null) await CardPileCmd.Draw(ctx, draw, who.Player);
                break;
            }
            case Errand.Fetch:
                await Fetch(ctx, falconer, who);
                if (Has(player, Trait.Magpie) && Glove(player)?.TakeMagpie() == true && player != null)
                    await PlayerCmd.GainGold(MagpieGold, player);
                break;
            case Errand.Harry:
            {
                var target = harry is { IsAlive: true } ? harry : RandomEnemy(falconer);
                dealt += await HitEnemy(ctx, falconer, target, Scale(HarryDamage + flat, s));
                break;
            }
            case Errand.Forage:
                if (who.Player != null) await PlayerCmd.GainEnergy(1, who.Player);
                await CreatureCmd.Heal(who, Math.Max(1, Scale(ForageHeal + flat, s)));
                break;
        }

        var wild = falconer.Powers.OfType<WildHeartPower>().FirstOrDefault();
        if (wild != null && dealt > 0) await CreatureCmd.GainBlock(falconer, dealt, ValueProp.Unpowered, null);
        return dealt;
    }

    /// <summary>While Harrying, at the start of each turn: Weak (and Vulnerable with Terror of the Skies).</summary>
    public static async Task HarryTick(PlayerChoiceContext ctx, Creature falconer, Creature? target)
    {
        if (target is not { IsAlive: true }) return;
        await PowerCmd.Apply<WeakPower>(ctx, target, 1, falconer, null);
        if (Has(falconer.Player, Trait.TerrorOfTheSkies))
            await PowerCmd.Apply<VulnerablePower>(ctx, target, 1, falconer, null);
    }

    /// <summary>+1 Scout draw and Block at Level 4, 7 and 10.</summary>
    private static int LevelSteps(Player? p)
    {
        var l = Level(p);
        return (l >= 4 ? 1 : 0) + (l >= 7 ? 1 : 0) + (l >= 10 ? 1 : 0);
    }

    private static async Task Fetch(PlayerChoiceContext ctx, Creature falconer, Creature who)
    {
        var p = who.Player;
        var discard = p?.PlayerCombatState?.DiscardPile;
        if (p == null || discard == null || discard.Cards.Count == 0) return;
        CardModel? card;
        if (who == falconer)
        {
            var chosen = await CardSelectCmd.FromCombatPile(ctx, discard, p,
                new CardSelectorPrefs(new LocString("powers", "FALCONER-AWAY_POWER.fetchPrompt"), 1));
            card = chosen.FirstOrDefault();
        }
        else card = discard.Cards.LastOrDefault(); // for an ally: the card they discarded last
        if (card == null) return;
        await CardPileCmd.Add(card, MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand);
        card.EnergyCost.SetThisTurn(0);
    }

    // ---------------- Targets ----------------
    public static IReadOnlyList<Creature> Enemies(Creature c) =>
        c.CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];

    public static Creature? RandomEnemy(Creature falconer)
    {
        var list = Enemies(falconer);
        if (list.Count == 0) return null;
        return list[falconer.Player!.RunState.Rng.CombatCardGeneration.NextInt(0, list.Count)];
    }

    private static Creature? MarkedOrRandom(Creature falconer) =>
        Enemies(falconer).FirstOrDefault(e => e.Powers.OfType<QuarryMarkPower>().Any()) ?? RandomEnemy(falconer);

    private static async Task<int> HitEnemy(PlayerChoiceContext ctx, Creature falconer, Creature? target, int dmg)
    {
        if (target is not { IsAlive: true } || dmg <= 0) return 0;
        var mark = target.Powers.OfType<QuarryMarkPower>().FirstOrDefault();
        if (mark != null) dmg = dmg * (100 + mark.Amount) / 100;
        await CreatureCmd.Damage(ctx, target, dmg, ValueProp.Unpowered, falconer);
        return dmg;
    }
}
