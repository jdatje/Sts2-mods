# Test checklist (v0.1)

## Solo
- [ ] The Golem shows up on character select with a name, description and 80 HP.
- [ ] Starting deck is 4 Strike, 4 Defend, Slam, Clay Arms; relic is Heart of Clay.
- [ ] Every card shows readable text (no `{...}` or missing-text keys), including upgraded versions.
- [ ] Clay Arms: Strike deals 8 instead of 6.
- [ ] Heart of Clay: first Part played in a combat gives 6 Block; the second does not; resets next combat.
- [ ] Playing Boulder Fists while Clay Arms is installed **replaces** Clay Arms (only one Arms icon).
- [ ] Two different slots (e.g. Clay Arms + Rooted Legs) are both kept.
- [ ] Lantern Eye: hand is 6 cards next turn.
- [ ] Rooted Legs: Block at end of turn.
- [ ] Mud Core: Defend gives 7; Rooted Legs/Boulder Fists Block is NOT increased.
- [ ] Furnace Core: one extra Energy each turn.
- [ ] Assemble / Full Body Slam numbers go up with each Part, and the in-combat preview matches.
- [ ] Collapse removes all Part icons and gives 7 Block per Part.
- [ ] Pebble Barrage hits 3 times; Avalanche hits all enemies 3 times.
- [ ] Save and quit mid-combat, continue: Parts are still there.

## Co-op (Jeroen + Gregor)
- [ ] Both have BaseLib and the same Golem version; the lobby lets you in.
- [ ] Lend a Hand only appears in co-op runs and gives Block to the other player.
- [ ] Play a full act with one Golem and one vanilla character: no "desync" message.
- [ ] Both play the Golem at once (two sets of Parts) for one act.

## If something breaks
Send the latest game log (Claude can find it on the linked PC), plus what you did just before.
