using BaseLib.Abstracts;
using BaseLib.Extensions;
using Bard.BardCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Visuals;

/// <summary>
/// Base for the starter relic and its Ancient upgrade. Besides their own effect, they drive the
/// character's on-screen motion (CreatureAnimator).
/// </summary>
public abstract class AnimatedStarterRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            CreatureAnimator.Start(Owner.Creature);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return Task.CompletedTask;
        if (cardPlay.Card.Type == CardType.Attack) CreatureAnimator.Attack(Owner.Creature);
        else CreatureAnimator.Cast(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner.Creature) CreatureAnimator.Hit(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task BeforeDeath(Creature creature)
    {
        if (creature == Owner.Creature) CreatureAnimator.Die(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictoryEarly(CombatRoom room)
    {
        CreatureAnimator.Stop(Owner.Creature);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CreatureAnimator.Stop(Owner.Creature);
        return Task.CompletedTask;
    }
}
