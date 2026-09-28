# Copilot code-review instructions

This repo is a BepInEx 6 (IL2CPP) Harmony mod for *Survival Log*. Plugins derive from `BasePlugin` and call the game through Il2CppInterop proxy assemblies. Review with these traps in mind; a general C# review misses most of them.

## IL2CPP Harmony traps

- **Getter/setter patches often never fire.** il2cpp inlines trivial accessors, so a `MethodType.Getter`/`.Setter` patch silently does nothing. Flag a new getter patch used as the only mechanism. The reliable change is mutating the backing field at a load hook.
- **No `is`/`as` across the interop boundary.** Flag `is`, `as`, or a direct cast on a game type. The correct form is `x.TryCast<T>()` then a null check.
- **No `foreach` over game collections.** The interop enumerator lacks the pattern. Expect `GetEnumerator()` / `MoveNext()` / `Current`, or a count plus an indexer.
- **Never read an `Il2CppSystem.ValueTuple<...>` result of a game method,** direct or as a list element. The interop layer reads the fields wrongly and gives garbage with no error. Expect a method that returns a class, a dictionary, or an `Il2CppStructArray`, or the value calculated in the mod.
- **Guard game lookups.** Singletons and config lookups return null often. Flag an unchecked dereference inside a patch.
- **A patch must not break the game.** Each patch body sits in a try/catch that logs the error once (`Plugin.WarnOnce`), so the workbench falls back to the game's own fill.

## Structure and tests

- **Feature folders.** Each feature has its own folder under `src/` with its patches, its game-facing code, and its game-free `*Logic.cs` file. The first feature, Craft from any storage, is `src/AnyStorage/`. `Plugin.cs` holds only the config, `Load`, and the shared helpers. One patch class for each target method, named `<Feature>On<Target>`.
- **A feature is patched all or nothing.** `AnyStorageFeature.Patch` resolves each target method first and attaches the patches only when all resolve, because a part of the feature alone misleads the player (a recipe list that counts home storage with no fill from it). Flag a new patch of the feature that is not in its target table.
- **The feature is off when BaseButler is loaded.** `Plugin` has a soft `BepInDependency` on `com.basebutler.mod`, and `AnyStorageFeature.Patch` attaches nothing when that plugin is in `IL2CPPChainloader.Instance.Plugins`. Flag a change that removes the check or moves it after the patches attach.
- **Pure logic is separated and tested.** Logic that does not need the running game (the recipe key, the missing materials, the pick rule and its order, the move steps, the recount of the recipe list JSON) lives in `src/AnyStorage/AnyStorageLogic.cs`, with no BepInEx or Il2Cpp reference, unit-tested under `tests/BetterCrafting.Tests`. Flag new pure logic in a game-facing file, and new pure logic with no test.
- **Patches** prefer a postfix, and tie the `Harmony` instance to the plugin GUID. A Prefix skips the game only in the mod's own case: the game's "not enough materials" text for a click that the mod fills, and the game's Craft Again fill when the mod fills from home storage.

## Game rules the mod keeps

- **Only home storage.** The sources are the game's own (the bag, the workbench drawer, the Tool Cabinets) and the furniture that passes the game's `ItemManager.IsInHomeScope`, the rule of the seeds in the planting window. Flag a source that skips this rule, for example a list of all furniture or of all furniture with a bag.
- **The pick order.** The bag first, then the workbench drawer and the Tool Cabinets, then the other home storage. In each group, a clean item before a polluted one, and the item that expires first before a fresher one. The fill moves nothing when the home does not have each material.
- **Moves go through the game.** The fill moves items only with the game's `SyncWorkbenchTo`, with one call for each source owner, because the call takes each new item from its `leftOwnerId`. A move with a wrong owner leaves a stale item in the source. Flag a direct change of an item's owner or a move of items from more than one owner in one call.
- **Fill only.** The mod never starts a craft. The player starts it with the game's button.
- **The recipe list count finds each material by its name.** The game lists the materials of a recipe in the order of the recipe config, not by item id, so `RecountMaterialsJson` takes the counts by the name that the game writes (`ConstantTextTools.GetLocalText(Config_Item.ItemName)`). Flag a recount by position.
- **No page script, no game file, no save data.** The mod changes only the game's own values in the workbench window state. Flag a write to a game file, to the save, or a new page script.

## Release and config hygiene

- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is `com.ivmakk.survivallog.bettercrafting`, the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, or a game DLL in the diff. Game `<Reference>` entries keep `<Private>false</Private>`.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.
