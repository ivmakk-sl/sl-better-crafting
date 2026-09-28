# Better Crafting

Better Crafting is a mod for the Steam game *Survival Log*. Its first feature, **Craft from any storage**, lets the workbench take the materials of a recipe from all storage in your home, not only from your backpack, the Workbench Drawer, and the Tool Cabinets.

Without the mod, the workbench fills a recipe only from the backpack (the Inventory tab of the workbench window), the Workbench Drawer, and the Tool Cabinets. Materials in the other cabinets and racks of your home must first go into the backpack by hand. With Better Crafting, one click on a recipe fills the workbench grid from all of them.

## Craft from any storage

- **One click fills the grid.** Click a recipe in Crafting Notes. When the game's own sources do not have all the materials, the mod moves the missing materials to the workbench grid from the other storage of your home. You do not open a storage tab or move items by hand.
- **Your home, and only your home.** The mod uses the same storage as the planting window uses for seeds: the storage on your home floor, and on each floor and area of your home that is unlocked. It never takes items from a locked floor or area, or from a place outside your home.
- **A fixed pick order.** The mod takes the materials from your backpack first, then from the Workbench Drawer and the Tool Cabinets, then from the other storage. In each place, it takes clean items before polluted items, and the items that expire first before fresher items.
- **All or nothing.** When your home does not have enough of each material, the mod moves nothing, and the game shows its usual message for missing materials.
- **The recipe list counts your home.** The "have / need" count of each material and the brown border of a recipe with enough materials include the storage of your home. A recipe that the game locks stays locked.
- **Craft Again works.** After a craft, Craft Again fills the grid from the same places.
- **Fill only.** The mod never starts a craft. You check the grid and click Craft.

The mod moves items only with the game's own move to the workbench grid. It changes no game file and writes nothing of its own to the save, so you can remove it at any time.

## Compatibility

Does not work together with BaseButler. When BaseButler is installed, Craft from any storage turns itself off, and the log says so.

## Requirements

The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\BetterCrafting.dll`

## Uninstall

Delete `BetterCrafting.dll` from the `BepInEx\plugins` folder. On the next game start, the workbench works as without the mod.

## Configuration

The config file is `BepInEx\config\com.ivmakk.survivallog.bettercrafting.cfg`. It has one setting, `Verbose` (default `false`), which logs each fill of the mod at Debug level. It is for troubleshooting only. The mod has no setting for players.

## Troubleshooting

If a game update removes a part of the workbench that the mod needs, Craft from any storage turns itself off, and the workbench works as without the mod.

Check `BepInEx\LogOutput.log` for the `Better Crafting loaded.` line. Check for warnings or errors from Better Crafting.

## Build

Better Crafting is a BepInEx 6 IL2CPP plugin. Building requires the .NET 8 SDK and a game installation with BepInEx. Start the game once after installing BepInEx to generate the IL2CPP interop assemblies. These assemblies come from the game and are not included in this repository.

```
dotnet build src/BetterCrafting.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam installation path. For another location, set the `GameDir` environment variable or pass `-p:GameDir=...` to the build command. The output DLL is `src\bin\Release\BetterCrafting.dll`.

Unit tests cover the recipe key, the missing materials, the pick rule and its order, the move steps, and the recount of the recipe list. The tested code has no game dependencies. It lives in `src/AnyStorage/AnyStorageLogic.cs`.

```
dotnet test tests/BetterCrafting.Tests
```

## Package

Add `-p:Package=true` to a Release build to create `dist\BetterCrafting-<version>.zip`. The zip contains `BepInEx\plugins\BetterCrafting.dll`, ready to extract into the game folder. A plain build does not create the zip.

```
dotnet build src/BetterCrafting.csproj -c Release -p:Package=true
```

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.
