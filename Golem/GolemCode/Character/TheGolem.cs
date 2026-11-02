using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Golem.GolemCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Golem.GolemCode.Cards;
using Golem.GolemCode.Relics;

namespace Golem.GolemCode.Character;

public class TheGolem : PlaceholderCharacterModel
{
    public const string CharacterId = "TheGolem";
    
    public static readonly Color Color = new("a0785a"); // muddy clay brown

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 80;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeGolem>(),
        ModelDb.Card<StrikeGolem>(),
        ModelDb.Card<StrikeGolem>(),
        ModelDb.Card<StrikeGolem>(),
        ModelDb.Card<DefendGolem>(),
        ModelDb.Card<DefendGolem>(),
        ModelDb.Card<DefendGolem>(),
        ModelDb.Card<DefendGolem>(),
        ModelDb.Card<Slam>(),
        ModelDb.Card<ClayArms>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<HeartOfClay>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<GolemCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<GolemRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<GolemPotionPool>();
    
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
    public override string CustomIconTexturePath => "character_icon_golem.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_golem.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_golem_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_golem.png".CharacterUiPath();
}