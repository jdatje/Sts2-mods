# The Falconer

A hunter and her peregrine falcon. Design: Claude Doc `claude/falconer-design.md` in the STS2 Mods project.

- **Falcon:** never targeted, no HP. `Send <Errand>` puts it Away (`AwayPower`, amount = turns left); at 0 it returns and the Errand resolves (`Falcon/FalconSystem.cs`).
- **Perched / Alone:** card bonuses while the falcon is home / away. `Recall` brings it back at 50%.
- **Run scaling:** XP and Traits are `[SavedProperty]` on the starter relic (`GloveRelic`). Level 1–10, +10% Errand strength per Level. Rest site option **Fly** (`FlyRestSiteOption`) picks 1 of 3 Traits.
- **Co-op:** Send cards that target an ally make the Errand work for that ally.
- **On screen:** `Falcon/FalconOverlay.cs` puts the falcon sprite on the shoulder and flies it off and back.
- Known limit: the current Errand's details are not saved mid-combat (after a reload, an Away falcon returns as a Hunt).
