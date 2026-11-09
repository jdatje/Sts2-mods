using Falconer.FalconerCode.Falcon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Falconer.FalconerCode.Powers;

/// <summary>
/// The falcon is Away on an Errand. Amount = turns until it returns.
/// At the start of your turn the count goes down; at 0 the Errand resolves and the falcon is Perched again.
/// </summary>
public sealed class AwayPower : FalconerPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public Errand Errand;
    public Creature? For;
    public Creature? HarryTarget;
    public int ExtraPct;
    public int Flat;
    public bool Chain;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player) return;
        if (Errand == Errand.Harry) await FalconCmd.HarryTick(ctx, Owner, HarryTarget);
        if (FalconCmd.Has(player, Trait.Patient))
            await CreatureCmd.GainBlock(Owner, 2, ValueProp.Unpowered, null);
        if (Amount <= 1)
        {
            Flash();
            await FalconCmd.ReturnNow(ctx, Owner, 100);
        }
        else await PowerCmd.ModifyAmount(ctx, this, -1, Owner, null, silent: true);
    }

    /// <summary>Bells, Long Flight: change the turns left (minimum 1).</summary>
    public async Task ChangeTurns(PlayerChoiceContext ctx, int delta)
    {
        var target = Math.Max(1, Amount + delta);
        if (target != Amount) await PowerCmd.ModifyAmount(ctx, this, target - Amount, Owner, null, silent: true);
    }
}

/// <summary>Simple stacking buff with helpers.</summary>
public abstract class SimpleFalconerPower : FalconerPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected bool Mine(Player player) => player == Owner.Player;
}

/// <summary>Whenever the falcon returns, gain X Strength (max 3 times per combat).</summary>
public sealed class FalconsSharePower : SimpleFalconerPower, IFalconListener
{
    private int _times;
    public async Task AfterReturn(PlayerChoiceContext ctx, Errand errand, int dealt)
    {
        if (_times >= 3) return;
        _times++;
        Flash();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>At the start of your turn, if Perched, gain X Block.</summary>
public sealed class GloveUpPower : SimpleFalconerPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!Mine(player) || !FalconCmd.Perched(Owner)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Whenever the falcon leaves, gain X Vigor.</summary>
public sealed class HuntersPatiencePower : SimpleFalconerPower, IFalconListener
{
    public async Task AfterSend(PlayerChoiceContext ctx, Errand errand)
    {
        Flash();
        await PowerCmd.Apply<VigorPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>Whenever you Send the falcon, gain X Block.</summary>
public sealed class HawkBellsPower : SimpleFalconerPower, IFalconListener
{
    public async Task AfterSend(PlayerChoiceContext ctx, Errand errand)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>Errands are X% stronger.</summary>
public sealed class ImprintPower : SimpleFalconerPower, IFalconListener
{
    public int ErrandBonusPct => Amount;
}

/// <summary>This combat, Errands are X% stronger (Molting Oil).</summary>
public sealed class MoltPower : SimpleFalconerPower, IFalconListener
{
    public int ErrandBonusPct => Amount;
}

/// <summary>Whenever the falcon returns, draw X cards.</summary>
public sealed class FlightFeathersPower : SimpleFalconerPower, IFalconListener
{
    public async Task AfterReturn(PlayerChoiceContext ctx, Errand errand, int dealt)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}

/// <summary>Perched and Alone bonuses both always count.</summary>
public sealed class PartnersPower : FalconerPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>Debuff: Errand damage against this enemy is X% higher, and Hunt goes for it.</summary>
public sealed class QuarryMarkPower : FalconerPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>At the end of your turn, if Alone, gain X Block.</summary>
public sealed class WeatheredPower : SimpleFalconerPower
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !FalconCmd.Alone(Owner)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>At the start of your turn, if Perched, Send Hunt.</summary>
public sealed class PeregrinePower : SimpleFalconerPower
{
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext ctx, Player player)
    {
        if (!Mine(player) || FalconCmd.IsAway(Owner)) return;
        Flash();
        await FalconCmd.Send(ctx, Owner, Errand.Hunt);
    }
}

/// <summary>Errands resolve twice.</summary>
public sealed class SkySovereignPower : FalconerPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>Whenever the falcon returns, draw X and gain X energy.</summary>
public sealed class FalconersPactPower : SimpleFalconerPower, IFalconListener
{
    public async Task AfterReturn(PlayerChoiceContext ctx, Errand errand, int dealt)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}

/// <summary>At the start of your turn, if Alone, gain X energy.</summary>
public sealed class LoneWolfPower : SimpleFalconerPower
{
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext ctx, Player player)
    {
        if (!Mine(player) || !FalconCmd.Alone(Owner)) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }
}

/// <summary>At the start of your turn, if Perched, gain X Vigor.</summary>
public sealed class BondOfTheHuntPower : SimpleFalconerPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!Mine(player) || !FalconCmd.Perched(Owner)) return;
        Flash();
        await PowerCmd.Apply<VigorPower>(ctx, Owner, Amount, Owner, null);
    }
}

/// <summary>Errand damage also gives you that much Block (handled in FalconCmd.Resolve).</summary>
public sealed class WildHeartPower : FalconerPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>Hunt deals X more damage and hits twice (handled in FalconCmd.Resolve).</summary>
public sealed class GyrfalconPower : SimpleFalconerPower;
