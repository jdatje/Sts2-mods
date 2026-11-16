using Bard.BardCode.Songs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Bard.BardCode.Cards;

// ================= Stanzas (offered after a big Song; never in a deck) =================

public abstract class StanzaCard : BardCard
{
    protected StanzaCard(Stanza stanza) : base(0, CardType.Power, CardRarity.Token, TargetType.None)
    {
        Stanza = stanza;
        WithVar(new StringVar("SongName", "your Song"));
    }

    public Stanza Stanza { get; }
    protected override bool IsPlayable => false;
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;

    public static StanzaCard Create(Stanza s, int song, Player owner)
    {
        StanzaCard card = s switch
        {
            Stanza.OpeningLine => owner.RunState.CreateCard<StanzaOpeningLine>(owner),
            Stanza.Echo => owner.RunState.CreateCard<StanzaEcho>(owner),
            Stanza.Resonant => owner.RunState.CreateCard<StanzaResonant>(owner),
            Stanza.BrightRefrain => owner.RunState.CreateCard<StanzaBrightRefrain>(owner),
            Stanza.GrandFinale => owner.RunState.CreateCard<StanzaGrandFinale>(owner),
            _ => owner.RunState.CreateCard<StanzaLingering>(owner),
        };
        if (card.DynamicVars.TryGetValue("SongName", out var v) && v is StringVar name)
            name.StringValue = SongBook.Names[song];
        return card;
    }
}

public sealed class StanzaOpeningLine() : StanzaCard(Stanza.OpeningLine);
public sealed class StanzaEcho() : StanzaCard(Stanza.Echo);
public sealed class StanzaResonant() : StanzaCard(Stanza.Resonant);
public sealed class StanzaBrightRefrain() : StanzaCard(Stanza.BrightRefrain);
public sealed class StanzaGrandFinale() : StanzaCard(Stanza.GrandFinale);
public sealed class StanzaLingering() : StanzaCard(Stanza.Lingering);
