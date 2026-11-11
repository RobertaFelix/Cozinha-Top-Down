# Editor developer console

Press **F8** during Editor Play Mode, or use **UnderGrub > Developer Console**. The console is an overlay inside the Game view. F8 or Esc closes it. Gameplay/input is suspended while it is open; the prior time scale and enabled input scripts are restored on close. Enter runs a command, clears the field and returns focus to it on the next frame, so consecutive commands need no extra click. Commands that change game state require Play Mode, a ready save service, and no scene transition, item drag or end-of-day screen.

The overlay is created dynamically only during Editor Play Mode. Every source is under `Assets/Editor/DevConsole` and additionally wrapped in `#if UNITY_EDITOR`; no console object is serialized in a scene or prefab, and no player input hook or console code is included in a player. The build preprocessor rejects console source files in player assemblies. Run **UnderGrub > Developer Console Checks > Check player exclusion** to inspect the actual Unity compilation manifests.

## Commands

- `help` or `help money`: list commands and usage.
- `status`: current scene, position, coins, day and time.
- `clear`: clear console output. Up/Down browse the last 100 commands; Tab completes a command name.
- `money add 500`, `money remove 100`, `money set 0`: change exact amounts, independent of reward multipliers. Negative balances and integer overflow are rejected.
- `items` or `items potion`: list prefab IDs, display names, types and GUIDs from the item catalog.
- `give garlic 10`, `give "Cyan Potion" 2 hotbar`: add to inventory (default) or hotbar. Quote display names containing spaces. The prefab ID or stable GUID also works.
- `remove garlic 3`, `remove cyan 1 hotbar`: remove only from that container; insufficient quantities leave it unchanged.
- `dish give dinner01 2`, `dish remove dinner01 1`: cooked-dish equivalents, with the same optional quantity/container arguments. `give` and `remove` also accept dishes.
- `inventory` or `inventory hotbar`: show exact slot contents.
- `tp -3.25 7.5`: teleport to coordinates in the current environment; optional third argument is Z. Coordinates use a dot decimal separator. This is a debug teleport and does not validate floor collisions.
- `tp home`, `tp village`, `tp restaurant`, `tp market`, `tp dungeon`: use existing spawn markers and scene/environment transitions. Dungeon entry uses the normal random layout/spawn selection. Return to the village before choosing market/restaurant from a dungeon.
- `recipes`, `recipes dinner01`, `recipe unlock dinner01`, `recipe unlock all`: inspect/unlock configured village recipe-book entries. Recipes outside the current book slot count are listed separately; use `dish give` to create their outputs directly.
- `upgrade list`, `upgrade add backpack`, `upgrade remove 2`, `upgrade add key`: grant/revoke implemented upgrades without payment. Existing stacks are preserved when revoking the backpack.
- `time set 07:00`, `day set 5`, `day add 1`: change the clock; day numbers are one-based and preserve the time of day.
- `save`: explicitly write the current checkpoint, including position. Village scene only, matching the current save system.
- `save path`: show the checkpoint file path.
- `save backup`: copy known save files to a uniquely named `ManualBackups` folder.
- `save reset`: archive known checkpoint files under `ResetBackups`, unload the persistent game root, clear upgrades and load a fresh first scene. It does not delete the archived save. If archiving fails, the game is not reset; partially moved files are rolled back. It also works when invoked from the dungeon or when an incompatible/corrupt save has blocked startup. `save path` and `save backup` are also available while that error screen is open.
- `load`: reload the last checkpoint in the village scene. Loading a pending end-of-day checkpoint keeps that screen paused when the console closes.

Changes are not automatically written to disk: use the bed or `save`. Save reset is explicitly requested by its command and is never performed by opening/closing the console or by building the player.

## Extending

Register a new verb with `DevConsoleCommands.Register(name, usage, description, handler, readOnly)`. Handlers receive parsed arguments and a result/output object. Keep extensions in the Editor folder and behind the Editor define. Tokenization accepts single/double quoted arguments, escaped quotes/backslashes, and rejects malformed input; numeric commands validate before mutating gameplay.
