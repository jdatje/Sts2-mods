using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Bard.BardCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Bard.BardCode.Cards;
using Bard.BardCode.Relics;

namespace Bard.BardCode.Character;

public class TheBard : PlaceholderCharacterModel
{
    public const string CharacterId = "TheBard";
    
    public static readonly Color Color = new("7b4fa0"); // velvet purple

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

    public override string PlaceholderID => "regent";
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeBard>(), ModelDb.Card<StrikeBard>(), ModelDb.Card<StrikeBard>(), ModelDb.Card<StrikeBard>(),
        ModelDb.Card<DefendBard>(), ModelDb.Card<DefendBard>(), ModelDb.Card<DefendBard>(), ModelDb.Card<DefendBard>(),
        ModelDb.Card<BattleHymn>(),
        ModelDb.Card<Hum>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<WornLute>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<BardCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<BardRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<BardPotionPool>();
    
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
    public override string CustomIconTexturePath => "character_icon_bard.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_bard.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_bard_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_bard.png".CharacterUiPath();

    // In-combat body: a single painted image (transparent PNG). BaseLib builds the hitbox from the image size.
    public override NCreatureVisuals? CreateCustomVisuals() =>
        NodeFactory<NCreatureVisuals>.CreateFromResource("combat_bard.png".CharacterUiPath());

    // Character select backdrop (full screen, like the base characters).
    public override string CustomCharacterSelectBg => MainFile.ResPath + "/scenes/char_select_bg_bard.tscn";
}
