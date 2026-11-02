using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Golem.GolemCode.Parts;

// All Parts: buff, Counter stacking (so the amount is shown on the icon).
// Stacking never actually happens: PartCmd.Install removes the old Part first.

/// <summary>ARMS: Your Attacks deal X additional damage.</summary>
public sealed class ClayArmsPower : PartPower
{
    public override PartSlot Slot => PartSlot.Arms;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner || cardSource == null || !props.IsPoweredAttack()) return 0m;
        return Amount;
    }
}

/// <summary>ARMS: Whenever you play an Attack, gain X Block.</summary>
public sealed class BoulderFistsPower : PartPower
{
    public override PartSlot Slot => PartSlot.Arms;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Attack) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>HEAD: Draw X additional card(s) each turn.</summary>
public sealed class LanternEyePower : PartPower
{
    public override PartSlot Slot => PartSlot.Head;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player) return count;
        return count + Amount;
    }
}

/// <summary>LEGS: At the end of your turn, gain X Block.</summary>
public sealed class RootedLegsPower : PartPower
{
    public override PartSlot Slot => PartSlot.Legs;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>CORE: Block you gain from cards is increased by X.</summary>
public sealed class MudCorePower : PartPower
{
    public override PartSlot Slot => PartSlot.Core;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || cardSource == null || !props.IsPoweredCardOrMonsterMoveBlock()) return 0m;
        return Amount;
    }
}

/// <summary>CORE: At the start of your turn, gain X Energy.</summary>
public sealed class FurnaceCorePower : PartPower
{
    public override PartSlot Slot => PartSlot.Core;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }
}
