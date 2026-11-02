using BaseLib.Abstracts;
using Bard.BardCode.Extensions;
using Godot;

namespace Bard.BardCode.Character;

public class BardPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => TheBard.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}