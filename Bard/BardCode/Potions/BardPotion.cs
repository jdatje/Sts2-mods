using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Bard.BardCode.Character;
using Bard.BardCode.Extensions;

namespace Bard.BardCode.Potions;

[Pool(typeof(BardPotionPool))]
public abstract class BardPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}