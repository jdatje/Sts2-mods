using MegaCrit.Sts2.Core.Entities.Powers;

namespace CardSharp.CardSharpCode.Powers;

/// <summary>Showdowns happen at 4 cards instead of 5. Checked by TableCmd.</summary>
public sealed class CardSharkPower : CardSharpPower
{
    /// <summary>Showdowns with Card Shark deal this percentage of their damage.</summary>
    public const int DamagePercent = 80;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
