using Golem.GolemCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Rooms;

namespace Golem.GolemCode.Parts;

/// <summary>Shared defaults for all Parts: buff, amount shown on the icon.</summary>
public abstract class SimplePart : PartPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected IReadOnlyList<Creature> Enemies => CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];
    protected bool IsMine(CardPlay play) => play.Card.Owner == Owner.Player;
    protected bool IsMyTurnStart(Player player) => player == Owner.Player;
    protected bool IsMySideEnd(CombatSide side) => side == Owner.Side;
}

// ===================== HEAD =====================

/// <summary>HEAD: At the start of your turn, gain X Block.</summary>
public sealed class MossyCrownPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Head;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!IsMyTurnStart(player)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>HEAD: Draw X additional cards each turn (rare, bigger Lantern Eye).</summary>
public sealed class CrystalSkullPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Head;
    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player ? count + Amount : count;
}

/// <summary>HEAD: Whenever you play a Power, draw X cards.</summary>
public sealed class ThinkingStonePower : SimplePart
{
    public override PartSlot Slot => PartSlot.Head;
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsMine(play) || play.Card.Type != CardType.Power || Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

// ===================== ARMS =====================

/// <summary>ARMS: Your first Attack each turn applies X Vulnerable.</summary>
public sealed class IronHooksPower : SimplePart
{
    private bool _usedThisTurn;
    public override PartSlot Slot => PartSlot.Arms;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (IsMyTurnStart(player)) _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (_usedThisTurn || !IsMine(play) || play.Card.Type != CardType.Attack || play.Target == null) return;
        _usedThisTurn = true;
        Flash();
        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target, Amount, Owner, null);
    }
}

/// <summary>ARMS: Whenever you play an Attack, deal X damage to ALL enemies.</summary>
public sealed class SpikedKnucklesPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Arms;
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsMine(play) || play.Card.Type != CardType.Attack || Enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx, Enemies, Amount, ValueProp.Unpowered, Owner);
    }
}

/// <summary>ARMS: Your Attacks deal X additional damage (rare, bigger Clay Arms).</summary>
public sealed class HammerArmsPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Arms;
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        dealer == Owner && cardSource != null && props.IsPoweredAttack() ? Amount : 0m;
}

// ===================== LEGS =====================

/// <summary>LEGS: Whenever you play a Skill, gain X Block.</summary>
public sealed class SpringLegsPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Legs;
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsMine(play) || play.Card.Type != CardType.Skill) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>LEGS: At the end of your turn, deal X damage to ALL enemies.</summary>
public sealed class StompingLegsPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Legs;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsMySideEnd(side) || Enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx, Enemies, Amount, ValueProp.Unpowered, Owner);
    }
}

/// <summary>LEGS: At the end of your turn, gain X Block for each Part you have.</summary>
public sealed class IronTreadsPower : SimplePart
{
    public const int DecayPerTurn = 2;
    private int _turns;
    public override PartSlot Slot => PartSlot.Legs;
    public override Task OnInstalled(PlayerChoiceContext ctx)
    {
        _turns = 0;
        return Task.CompletedTask;
    }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsMySideEnd(side)) return;
        var block = Amount * PartCmd.CountAll(Owner) - DecayPerTurn * _turns++;
        if (block <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}

/// <summary>LEGS: Block is not removed at the start of your turn (adds Barricade while installed).</summary>
public sealed class UnmovableLegsPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Legs;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task OnInstalled(PlayerChoiceContext ctx)
    {
        if (!Owner.HasPower<BarricadePower>())
            await PowerCmd.Apply<BarricadePower>(ctx, Owner, 1, Owner, null);
    }

    public override async Task OnUninstalled(PlayerChoiceContext ctx) =>
        await PowerCmd.Remove<BarricadePower>(Owner);
}

// ===================== CORE =====================

/// <summary>CORE: At the end of your turn, gain X Plating.</summary>
public sealed class StoneHeartPower : SimplePart
{
    public override PartSlot Slot => PartSlot.Core;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsMySideEnd(side)) return;
        Flash();
        await PowerCmd.Apply<PlatingPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>CORE: Your Attacks deal X additional damage. At the end of your turn, lose 1 HP.</summary>
public sealed class MagmaCorePower : SimplePart
{
    public override PartSlot Slot => PartSlot.Core;
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        dealer == Owner && cardSource != null && props.IsPoweredAttack() ? Amount : 0m;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsMySideEnd(side)) return;
        await CreatureCmd.Damage(ctx, Owner, 1, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
    }
}

/// <summary>CORE: At the start of your turn, gain X Strength.</summary>
public sealed class GeodeCorePower : SimplePart
{
    public override PartSlot Slot => PartSlot.Core;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!IsMyTurnStart(player)) return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>CORE: Whenever you play an Attack, deal X damage to ALL enemies.</summary>
public sealed class VolcanicCorePower : SimplePart
{
    public override PartSlot Slot => PartSlot.Core;
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsMine(play) || play.Card.Type != CardType.Attack || Enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx, Enemies, Amount, ValueProp.Unpowered, Owner);
    }
}

// ===================== Non-Part powers =====================

public abstract class SimpleGolemPower : GolemPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>Whenever you install a Part, gain X Block.</summary>
public sealed class RockSolidPower : SimpleGolemPower, IPartListener
{
    public async Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Whenever you install a Part, draw X cards.</summary>
public sealed class AssemblyLinePower : SimpleGolemPower, IPartListener
{
    public async Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

/// <summary>Whenever you install a Part, gain X Strength and X Dexterity.</summary>
public sealed class PerfectFormPower : SimpleGolemPower, IPartListener
{
    public async Task AfterPartInstalled(PlayerChoiceContext ctx, PartPower part)
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner, Amount, Owner, null);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>At the end of your turn, gain X Plating for each Part you have.</summary>
/// <summary>At the end of your turn, raise your Plating to 1 per Part you have (all Parts count).</summary>
public sealed class LivingFortressPower : SimpleGolemPower
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        // Raise Plating to (Amount x Parts), counting all Parts (no cap of 4). Doesn't stack beyond that.
        var target = Amount * PartCmd.CountAll(Owner);
        var current = Owner.Powers.OfType<PlatingPower>().Sum(p => p.Amount);
        if (target <= current) return;
        Flash();
        await PowerCmd.Apply<PlatingPower>(ctx, Owner, target - current, Owner, null);
    }
}

/// <summary>At the end of your turn, heal X HP.</summary>
public sealed class SelfRepairPower : SimpleGolemPower
{
    /// <summary>At the end of combat (on victory), heal X HP.</summary>
    public override async Task AfterCombatVictoryEarly(CombatRoom room)
    {
        Flash();
        await CreatureCmd.Heal(Owner, Amount);
    }
}
