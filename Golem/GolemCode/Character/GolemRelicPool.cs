using BaseLib.Abstracts;
using Golem.GolemCode.Extensions;
using Godot;

namespace Golem.GolemCode.Character;

public class GolemRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => TheGolem.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}