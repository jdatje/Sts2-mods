using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Falconer.FalconerCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Falconer.FalconerCode.Cards;
using Falconer.FalconerCode.Relics;

namespace Falconer.FalconerCode.Character;

public class TheFalconer : PlaceholderCharacterModel
{
    public const string CharacterId = "TheFalconer";
    
    public static readonly Color Color = new("a8692e"); // falconer leather

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 72;

    public override string PlaceholderID => "silent";
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeFalconer>(), ModelDb.Card<StrikeFalconer>(), ModelDb.Card<StrikeFalconer>(), ModelDb.Card<StrikeFalconer>(),
        ModelDb.Card<DefendFalconer>(), ModelDb.Card<DefendFalconer>(), ModelDb.Card<DefendFalconer>(), ModelDb.Card<DefendFalconer>(),
        ModelDb.Card<LooseTheHawk>(),
        ModelDb.Card<Whistle>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<FalconersGlove>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<FalconerCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<FalconerRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<FalconerPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_falconer.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_falconer.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_falconer_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_falconer.png".CharacterUiPath();

    // In-combat body: a single painted image (transparent PNG). BaseLib builds the hitbox from the image size.
    public override NCreatureVisuals? CreateCustomVisuals() =>
        NodeFactory<NCreatureVisuals>.CreateFromResource("combat_falconer.png".CharacterUiPath());

    // Character select backdrop (full screen, like the base characters).
    public override string CustomCharacterSelectBg => MainFile.ResPath + "/scenes/char_select_bg_falconer.tscn";
    public override string CustomMerchantAnimPath => MainFile.ResPath + "/scenes/merchant_falconer.tscn";
}
