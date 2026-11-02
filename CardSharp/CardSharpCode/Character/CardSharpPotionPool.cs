using BaseLib.Abstracts;
using CardSharp.CardSharpCode.Extensions;
using Godot;

namespace CardSharp.CardSharpCode.Character;

public class CardSharpPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => TheCardSharp.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}