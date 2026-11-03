using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Bard.BardCode.Character;
using Bard.BardCode.Extensions;
using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Bard.BardCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(BardCardPool))]
public abstract class BardCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    // ---- Small helpers so card classes stay short ----
    protected Task Hit(PlayerChoiceContext ctx, CardPlay play, int hits = 1) =>
        CommonActions.CardAttack(this, play, hits).Execute(ctx);
    protected Task<decimal> Guard(CardPlay play) => CommonActions.CardBlock(this, play);
    protected Task DrawCards(PlayerChoiceContext ctx) => CommonActions.Draw(this, ctx);
    protected async Task Debuff<T>(PlayerChoiceContext ctx, CardPlay play) where T : PowerModel
    {
        if (play.Target != null) await CommonActions.Apply<T>(ctx, play.Target, this);
    }
    protected async Task DebuffAll<T>(PlayerChoiceContext ctx) where T : PowerModel
    {
        if (Enemies.Count > 0) await CommonActions.Apply<T>(ctx, Enemies, this);
    }
    protected Task Buff<T>(PlayerChoiceContext ctx) where T : PowerModel => CommonActions.ApplySelf<T>(ctx, this);
    protected async Task BuffAllies<T>(PlayerChoiceContext ctx) where T : PowerModel
    {
        foreach (var a in Allies) await CommonActions.Apply<T>(ctx, a, this);
    }
    protected async Task GuardAllies(CardPlay play, decimal amount)
    {
        foreach (var a in Allies) await CreatureCmd.GainBlock(a, amount, ValueProp.Move, play);
    }
    protected Task Energy(decimal amount) => PlayerCmd.GainEnergy(amount, Owner);
    protected Task<decimal> GainBlock(decimal amount, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Move, play);
    protected Task AddVerses(PlayerChoiceContext ctx, int n) => SongCmd.AddVerses(ctx, Owner.Creature, n);
    protected int Verses => SongCmd.Verses(Owner.Creature);
    protected bool Singing => SongCmd.Current(Owner.Creature) != null;
    protected IReadOnlyList<Creature> Enemies => CombatState?.HittableEnemies ?? (IReadOnlyList<Creature>)[];
    protected IReadOnlyList<Creature> Allies =>
        CombatState?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? (IReadOnlyList<Creature>)[];
    protected int V(string name) => DynamicVars[name].IntValue;

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}