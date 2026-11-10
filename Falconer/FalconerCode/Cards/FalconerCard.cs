using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Falconer.FalconerCode.Character;
using Falconer.FalconerCode.Extensions;
using Falconer.FalconerCode.Falcon;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Falconer.FalconerCode.Cards;

/// <summary>Base class for the Falconer's cards: image paths plus short helpers for the falcon.</summary>
[Pool(typeof(FalconerCardPool))]
public abstract class FalconerCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    /// <summary>
    /// Send cards are built with AnyAlly. AnyAlly has no valid target in solo (and leaves out yourself in co-op),
    /// so: solo → Self, co-op → AnyPlayer (yourself or an ally).
    /// </summary>
    public override TargetType TargetType
    {
        get
        {
            var t = base.TargetType;
            if (t != TargetType.AnyAlly) return t;
            var players = Owner?.RunState?.Players.Count ?? 1;
            return players > 1 ? TargetType.AnyPlayer : TargetType.Self;
        }
    }

    // ---- Falcon state ----
    protected Creature Me => Owner.Creature;
    protected bool Perched => FalconCmd.Perched(Owner.Creature);
    protected bool Alone => FalconCmd.Alone(Owner.Creature);
    protected bool Away => FalconCmd.IsAway(Owner.Creature);

    /// <summary>Send the falcon. For Send cards that target an ally, the Errand works for that ally.</summary>
    protected Task Send(PlayerChoiceContext ctx, Errand errand, CardPlay? play = null, Creature? harry = null,
        int extraPct = 0, int flat = 0, bool chain = false)
    {
        var forC = play?.Target is { } t && t.IsPlayer ? t : Me;
        return FalconCmd.Send(ctx, Me, errand, forC, harry, extraPct, flat, chain);
    }

    protected Task Recall(PlayerChoiceContext ctx, int? pct = null) => FalconCmd.Recall(ctx, Me, pct);

    /// <summary>Helpers for WithCalculatedDamage/Block conditions.</summary>
    protected static decimal PerchedOf(CardModel c, Creature? _) => FalconCmd.Perched(c.Owner.Creature) ? 1 : 0;
    protected static decimal AloneOf(CardModel c, Creature? _) => FalconCmd.Alone(c.Owner.Creature) ? 1 : 0;

    // ---- Small helpers so card classes stay short ----
    protected Task Hit(PlayerChoiceContext ctx, CardPlay play, int hits = 1) =>
        CommonActions.CardAttack(this, play, hits).Execute(ctx);
    protected Task<decimal> Guard(CardPlay play) => CommonActions.CardBlock(this, play);
    protected Task DrawCards(PlayerChoiceContext ctx) => CommonActions.Draw(this, ctx);
    protected Task Draw(PlayerChoiceContext ctx, int n) => CardPileCmd.Draw(ctx, n, Owner);
    protected async Task Debuff<T>(PlayerChoiceContext ctx, CardPlay play) where T : PowerModel
    {
        if (play.Target != null) await CommonActions.Apply<T>(ctx, play.Target, this);
    }
    protected async Task DebuffAll<T>(PlayerChoiceContext ctx) where T : PowerModel
    {
        if (Enemies.Count > 0) await CommonActions.Apply<T>(ctx, Enemies, this);
    }
    protected Task Buff<T>(PlayerChoiceContext ctx) where T : PowerModel => CommonActions.ApplySelf<T>(ctx, this);
    protected Task Energy(decimal amount) => PlayerCmd.GainEnergy(amount, Owner);
    protected Task<decimal> GainBlock(decimal amount, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Move, play);
    protected Task HitAll(PlayerChoiceContext ctx) =>
        CreatureCmd.Damage(ctx, Enemies, DynamicVars.Damage, Owner.Creature, this);
    protected IReadOnlyList<Creature> Enemies => CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];
    protected int V(string name) => DynamicVars[name].IntValue;

    //Normal art: 1000x760 (500x380 also works). Small variant: 250x190.
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.jpg".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.jpg".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.jpg".CardImagePath();
}

/// <summary>Base for simple power cards.</summary>
public abstract class FalconerPowerCard<T> : FalconerCard where T : PowerModel
{
    protected FalconerPowerCard(int cost, CardRarity rarity, int amount, int upgrade = 0, int costUpgrade = 0)
        : base(cost, CardType.Power, rarity, TargetType.Self)
    {
        WithPower<T>(amount, upgrade);
        if (costUpgrade != 0) WithCostUpgradeBy(costUpgrade);
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Buff<T>(ctx);
}
