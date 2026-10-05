# Copilot code-review instructions

This repo is a BepInEx 6 (IL2CPP) Harmony mod for *Survival Log*. Plugins derive from `BasePlugin` and call the game through Il2CppInterop proxy assemblies. Review with these traps in mind; a general C# review misses most of them.

## IL2CPP Harmony traps

- **Getter/setter patches often never fire.** il2cpp inlines trivial accessors, so a `MethodType.Getter`/`.Setter` patch silently does nothing. Flag a new getter patch used as the only mechanism. The reliable change is mutating the backing field at a load hook.
- **No `is`/`as` across the interop boundary.** Flag `is`, `as`, or a direct cast on a game type. The correct form is `x.TryCast<T>()` then a null check.
- **No `foreach` over game collections.** The interop enumerator lacks the pattern. Expect `GetEnumerator()` / `MoveNext()` / `Current`, or a count plus an indexer.
- **Never read an `Il2CppSystem.ValueTuple<...>` result of a game method,** direct or as a list element. The interop layer reads the fields wrongly and gives garbage with no error. Expect a method that returns a class, a dictionary, or an `Il2CppStructArray`, or the value calculated in the mod.
- **Guard game lookups.** Singletons and config lookups return null often. Flag an unchecked dereference inside a patch.
- **A patch must not break the game.** Each patch body sits in a try/catch that logs the error once (`Plugin.WarnOnce`) and leaves the game's result unchanged, so the workbench falls back to the game's own fill.
- **Data states come from `GetData`.** `ReduxUISystem.GetState<T>` reads only the window states (`State_Web_*`) and gives null for a data state. Read a `State_Data_*` state (for example `State_Data_Item`) with `ReduxUISystem.GetData<T>`. Flag `GetState` of a data state.

## Structure and tests

- **Feature folders.** Each feature has its own folder under `src/` with its patches, its game-facing code, and its game-free `*Logic.cs` file. The first feature, Craft from any storage, is `src/AnyStorage/`. `Plugin.cs` holds only the config, `Load`, and the shared helpers. One patch class for each target method, named `<Feature>On<Target>`.
- **A feature is patched all or nothing.** `AnyStorageFeature.Patch` resolves each target method first and attaches the patches only when all resolve, because a part of the feature alone misleads the player (a recipe list that counts home storage with no fill from it). The table also holds a required target with no patch: `GetLinkedOwnersInFillOrder`, which exists only in game 1.1, where the fill reads `GetAllLinkedOwnerIds`. Flag a new patch of the feature that is not in its target table, and the removal of the required target.
- **The feature is off when BaseButler is loaded.** `Plugin` has a soft `BepInDependency` on `com.basebutler.mod`, and `AnyStorageFeature.Patch` attaches nothing when that plugin is in `IL2CPPChainloader.Instance.Plugins`. Flag a change that removes the check or moves it after the patches attach.
- **Pure logic is separated and tested.** Logic that does not need the running game (the owner list: the game's owners first and in their order, then the home owners, each owner once, with 0 and the excluded owners left out) lives in `src/AnyStorage/AnyStorageLogic.cs`, with no BepInEx or Il2Cpp reference, unit-tested under `tests/BetterCrafting.Tests`. Flag new pure logic in a game-facing file, and new pure logic with no test.
- **Patches** prefer a postfix, and tie the `Harmony` instance to the plugin GUID. The fill has one patch: a Postfix of `Reducer_Web_ToolTable.GetAllLinkedOwnerIds` that appends the home storage owners to the game's list. The game builds a new list on each call, so the Postfix appends in place. Flag a patch that rebuilds a part of the game's fill or its text.
- **The count cache is the one exception to "no rebuilt counts".** It is a second target group (`CacheTargets` in `AnyStorageFeature`), patched all or nothing after the fill group and inside its own try/catch, so a missing target turns off only the cache and the game counts as in 1.1.0. A Prefix, Postfix, and Finalizer on `RefreshRecipeList` open and close the cache for one refresh of one frame (`CountCache.Active`). Inside it, Prefixes on `CountOwnedInPool` (the `Il2CppStructArray<long>` overload, named by its argument types, because the interop also has a `long[]` helper) and on `CheckMaterialSufficiencyAcrossLinked` set `__result` from one `CountTable` of the places and return false. Outside a refresh, with `CountCache = false`, or after an error, they return true and the game's body runs. `CountTable` copies the game's rules: owner 0 and ghost items are skipped, a count below 1 counts as 1, and the pick check copies `SelectItemsForRecipe` (stacks largest first, a stack is taken only when it is at most the rest of the need, a material with no stack fails also with a need of 0, no items at all fails). Flag a change of these rules without a matching test in `CountTableTests`, a cache that outlives its refresh or frame, and a table that is not rebuilt after the game repairs a ghost item.
- **One scan for each frame.** The game calls `GetAllLinkedOwnerIds` once for each recipe of a list refresh (more than a thousand times in one click). `Sources.HomeOwners` keeps the list of the current `Time.frameCount` and window (`CurrentFurnitureId`) and scans the furniture again only when one of them changes. It keeps only furniture with items (an entry in `State_Data_Item.OwnerCache`), because the game allocates a list for each owner that it reads. Flag a scan on each call, and a change that drops the filter.

## Game rules the mod keeps

- **Only home storage.** The appended owners are the furniture that passes the game's `ItemManager.IsInHomeScope`, the rule of the seeds in the planting window. The workbench, the furniture of the open window, and the game's own owners are left out. Flag a source that skips this rule, for example a list of all furniture or of all furniture with a bag.
- **The game's order.** The home owners go after the game's owners, so the game's fill takes from its own tabs first and picks the items by its own rule. The mod has no pick order of its own. Flag a reorder of the game's owners.
- **Moves and text stay the game's.** The game's `SyncWorkbenchTo` moves the items of each owner, the game's hint names only its tabs, and Clear gives an item from home storage to the open tab. Flag a direct change of an item's owner, a move of the mod's own, or a text of the mod.
- **Fill only.** The mod never starts a craft. The player starts it with the game's button, and a batch craft that the player starts goes on with the game's own next crafts.
- **No page script, no game file, no save data.** The mod changes only the game's list of places for the fill. Flag a write to a game file, to the save, or a new page script.

## Release and config hygiene

- **CountCache ships on.** The `Performance` / `CountCache` config binds with default `true`. It is the player's fallback to the counts of 1.1.0.
- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is `com.ivmakk.survivallog.bettercrafting`, the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, or a game DLL in the diff. Game `<Reference>` entries keep `<Private>false</Private>`.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.
