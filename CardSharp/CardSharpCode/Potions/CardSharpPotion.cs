using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Character;
using CardSharp.CardSharpCode.Extensions;

namespace CardSharp.CardSharpCode.Potions;

[Pool(typeof(CardSharpPotionPool))]
public abstract class CardSharpPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}