using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Golem.GolemCode.Parts;

public static class GolemKeywords
{
    /// <summary>Shown as "Part." at the top of every Part card, with a hover tip.</summary>
    [CustomEnum("Part")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Part;
}
