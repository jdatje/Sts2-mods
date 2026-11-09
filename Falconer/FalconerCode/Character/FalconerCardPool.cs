using BaseLib.Abstracts;
using Falconer.FalconerCode.Extensions;
using Godot;

namespace Falconer.FalconerCode.Character;

public class FalconerCardPool : CustomCardPoolModel
{
    public override string Title => TheFalconer.CharacterId; //This is not a display name.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    /* Card frame colour. BaseLib feeds this colour's HSV into the game's frame shader.
       Base characters use red, green, blue, pink and orange, so we stay clear of those. */
    public override Color ShaderColor => new("8a5a2b"); // leather brown
    
    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load Falconer/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("8a5a2b");
    
    public override bool IsColorless => false;
}