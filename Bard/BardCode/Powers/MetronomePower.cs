using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Bard.BardCode.Powers;

/// <summary>At the start of your turn, add Amount Verses to your Song.</summary>
public sealed class MetronomePower : BardPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || SongCmd.Current(Owner) == null) return;
        Flash();
        await SongCmd.AddVerses(choiceContext, Owner, (int)Amount);
    }
}
