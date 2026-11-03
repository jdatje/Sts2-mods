using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using CardSharp.CardSharpCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using CardSharp.CardSharpCode.Cards;
using CardSharp.CardSharpCode.Relics;

namespace CardSharp.CardSharpCode.Character;

public class TheCardSharp : PlaceholderCharacterModel
{
    public const string CharacterId = "TheCardSharp";
    
    public static readonly Color Color = new("c9a227"); // card-table gold

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 68;

    public override string PlaceholderID => "silent";
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeSharp>(), ModelDb.Card<StrikeSharp>(), ModelDb.Card<StrikeSharp>(), ModelDb.Card<StrikeSharp>(),
        ModelDb.Card<DefendSharp>(), ModelDb.Card<DefendSharp>(), ModelDb.Card<DefendSharp>(), ModelDb.Card<DefendSharp>(),
        ModelDb.Card<Ante>(),
        ModelDb.Card<CutTheDeck>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<MarkedDeck>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<CardSharpCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CardSharpRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CardSharpPotionPool>();
    
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
    public override string CustomIconTexturePath => "character_icon_cardsharp.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_cardsharp.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_cardsharp_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_cardsharp.png".CharacterUiPath();

    // In-combat body: a single painted image (transparent PNG). BaseLib builds the hitbox from the image size.
    public override NCreatureVisuals? CreateCustomVisuals() =>
        NodeFactory<NCreatureVisuals>.CreateFromResource("combat_cardsharp.png".CharacterUiPath());

    // Character select backdrop (full screen, like the base characters).
    public override string CustomCharacterSelectBg => MainFile.ResPath + "/scenes/char_select_bg_cardsharp.tscn";
}
