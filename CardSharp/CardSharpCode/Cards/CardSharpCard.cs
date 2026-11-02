using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CardSharp.CardSharpCode.Character;
using CardSharp.CardSharpCode.Extensions;
using CardSharp.CardSharpCode.Table;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CardSharp.CardSharpCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(CardSharpCardPool))]
public abstract class CardSharpCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, target)
{
    /// <summary>Fixed suit of this card: Spades attack, Hearts block, Diamonds draw/energy, Clubs debuff.</summary>
    public abstract Suit Suit { get; }

    /// <summary>How many times this card is laid on the Table when played (Double Down = 2).</summary>
    protected virtual int LayCount => 1;

    /// <summary>Current rank 2..14, or 0 when not in combat yet. Rolled each time it enters your hand.</summary>
    public int Rank { get; private set; }

    public void SetRank(int rank)
    {
        Rank = Math.Clamp(rank, Poker.MinRank, Poker.MaxRank);
    }

    /// <summary>Uses the run's combat RNG, so every player in co-op rolls the same value.</summary>
    public int EnsureRank()
    {
        if (Rank == 0) RollRank();
        return Rank;
    }

    private void RollRank() =>
        Rank = Owner.RunState.Rng.CombatCardGeneration.NextInt(Poker.MinRank, Poker.MaxRank + 1);

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
            RollRank();
        return Task.CompletedTask;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        var suit = Suit.ToString();
        description.Add("RankLine", Rank == 0 ? suit : $"{Poker.RankName(Rank)} of {suit}");
    }

    /// <summary>The card's own effect. The base class lays it on the Table afterwards.</summary>
    protected abstract Task Effect(PlayerChoiceContext ctx, CardPlay cardPlay);

    protected sealed override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await Effect(ctx, cardPlay);
        for (var i = 0; i < LayCount; i++)
            await TableCmd.Lay(ctx, this);
    }

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