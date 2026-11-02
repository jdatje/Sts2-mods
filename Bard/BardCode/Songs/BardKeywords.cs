using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Bard.BardCode.Songs;

public static class BardKeywords
{
    /// <summary>Shown as "Song." on top of Song cards.</summary>
    [CustomEnum("Song")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Song;

    /// <summary>Hover tip only (not printed on cards automatically).</summary>
    [CustomEnum("Verse")]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Verse;
}
