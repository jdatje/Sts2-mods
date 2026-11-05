using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Golem.GolemCode.Character;
using Golem.GolemCode.Extensions;

namespace Golem.GolemCode.Potions;

[Pool(typeof(GolemPotionPool))]
public abstract class GolemPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}