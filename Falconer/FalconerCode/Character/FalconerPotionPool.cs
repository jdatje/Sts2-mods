using BaseLib.Abstracts;
using Falconer.FalconerCode.Extensions;
using Godot;

namespace Falconer.FalconerCode.Character;

public class FalconerPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => TheFalconer.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}