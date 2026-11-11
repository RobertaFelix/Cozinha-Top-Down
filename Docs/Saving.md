# Saving and sleeping

The bed uses the existing E/proximity interaction. It writes an end-of-day checkpoint before showing the summary, pauses gameplay, and writes a second checkpoint when Continue advances the clock to the next morning. Continuing leaves the player in the same position. The default morning is 07:00 and is configurable on BedDayCycle. Closing on the summary resumes that summary; closing after Continue resumes that morning. There is no startup menu.

SaveGameService waits for inventory/UI initialization, loads once at startup, and restores the exact saved player position and active interior. Older checkpoints without a player-location section fall back to the configured home spawn; sleeping again writes the exact location. Saves live in `Application.persistentDataPath/Saves/save.json`; `.bak` retains the preceding checkpoint. Temporary writes are flushed and atomically replaced. Invalid primary data can recover the backup; if loading fails, gameplay stays paused with Retry and existing files are preserved. Sleeping is the checkpoint trigger; quitting alone does not save later progress.

## Adding a mechanic

Implement ISaveParticipant, or derive a component from SaveParticipant<TState>. Give it a unique, stable SaveKey, a SaveVersion, and a RestoreOrder. Own the serializable DTO, Capture, Validate and Restore inside that mechanic's module. Attach the participant to PersistentObjects beside SaveGameService. Validation runs for every known section before any state is changed; restore failure attempts to roll the participants back. Missing sections retain mechanic defaults, and unknown sections are kept when saving. Add explicit migrations to that module when its DTO changes. A newer unsupported file/section must be rejected rather than overwritten.

Modules implementing ISceneSaveParticipant also keep their live state in memory while their scene-side controllers are recreated during dungeon visits. This does not write a disk checkpoint; sleeping remains the save trigger.

Current modules cover clock, economy/upgrades, inventory and hotbar (including empty slot positions), unlocked recipes/menu, cooking progress/tools, the exact player location/interior, and the pending end-of-day state. The legacy vegetable/fridge/freezer sources are currently infinite supplies; their temporary browsing panels and customer NPCs are not serialized as player inventories.

Saved item references use asset GUIDs baked into `Resources/SaveItemCatalog.asset`, never display names or runtime instance IDs. The catalog contains inventory prefabs with ItemDragHandler. Run **UnderGrub > Save > Rebuild item catalog** after adding an item; the build preprocessor also rebuilds it. Keep item `.meta` files when moving or renaming assets. Recipe IDs currently use their result-item GUID, so each recipe must have a unique result item; introduce explicit recipe IDs if multiple recipes later share a result.

DayEndScreen exposes SummaryContent for future profit/XP widgets. BedDayCycle has DayEnded and NewDayStarted events for additional day-boundary mechanics. The initial summary deliberately displays no fabricated earnings or XP.

## Verification

**UnderGrub > Save > Run storage checks** exercises atomic round trips, backup recovery, interrupted temporary writes, and rejection of newer formats in an isolated temporary directory. Play checks should additionally cover: E near the bed, pending-summary restart, exact one-day advancement, restart after Continue, exact player coordinates/interior after restart and Continue, exact hotbar slots, coins/upgrades, recipe/menu, cooking state, and failed saves not advancing the day. Use SaveGameService.TestDirectoryOverride in Editor-only checks to avoid touching a player's save.
