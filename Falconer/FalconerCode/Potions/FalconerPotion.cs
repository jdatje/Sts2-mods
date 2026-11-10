using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Falconer.FalconerCode.Character;
using Falconer.FalconerCode.Extensions;

namespace Falconer.FalconerCode.Potions;

[Pool(typeof(FalconerPotionPool))]
public abstract class FalconerPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}