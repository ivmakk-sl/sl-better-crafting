# Better Crafting

Better Crafting is a mod for the Steam game *Survival Log*. Its first feature, **Craft from any storage**, lets the workbench take the materials of a recipe from all storage in your home, not only from your backpack, the Workbench Drawer, and the Tool Cabinets.

Without the mod, the workbench fills a recipe only from the backpack (the Inventory tab of the workbench window), the Workbench Drawer, and the Tool Cabinets (you can craft more from the [Tool Cabinet](https://survivallog.grandwiki.com/supplies/#item-14094) recipe). A material in another cabinet or rack of your home must first go into one of them by hand.

Nexus page: https://www.nexusmods.com/survivallog/mods/18

## Craft from any storage

- **One click fills the grid.** Click a recipe in Crafting Notes. When your backpack, the Workbench Drawer, and the Tool Cabinets do not have all the materials, the game's fill takes the rest from the other storage of your home. You do not open a storage tab or move items by hand.
- **Your home, and only your home.** The mod uses the same storage as the planting window uses for seeds: the storage on your home floor, and on each floor and area of your home that is unlocked. It never takes items from a locked floor or area, or from a place outside your home.
- **The game's order.** The fill takes the materials from the tab that is open on the left first, then from the game's other tabs (the backpack, the Workbench Drawer, and the Tool Cabinets), then from the other storage of your home. In each place, the game's own rule picks the items, so a polluted item or a fresh item can go to the grid. Keep an item that you want to save out of your home storage.
- **All or nothing.** When your home does not have enough of each material, the game moves nothing and shows its usual message for missing materials.
- **The recipe list counts your home.** The "have / need" count of each material, the brown border of a recipe with enough materials, and the "Max" of a batch craft include the storage of your home. A recipe that the game locks stays locked.
- **Craft Again, dyes, and batch crafts work.** Craft Again and a dye choice fill the grid from the same places. A batch craft fills each next craft from them too.
- **Clear sends items to the open tab.** When you click Clear, an item from the storage of your home goes to the tab that is open on the left, not back to its storage. The game's small origin mark still shows on that item on the grid.
- **The game's hint names only its tabs.** "Filled from other tabs" names the Workbench Drawer and the Tool Cabinets, not the storage of your home.
- **Fill only.** The mod never starts a craft. You check the grid and click Craft. A batch craft that you start goes on by itself, as in the game.

The mod adds the storage of your home to the game's own list of places for the fill, so the game fills, counts, and moves the items with its own code. It changes no game file and writes nothing of its own to the save, so you can remove it at any time.

## Compatibility

Does not work together with [BaseButler](https://www.nexusmods.com/survivallog/mods/4). When BaseButler is installed, Craft from any storage turns itself off, and the log says so.

## Requirements

- Survival Log 1.1.18153 or later. On an older game version, this version turns itself off. For game version 1.0.17573, use [Better Crafting 1.0.0](https://github.com/ivmakk-sl/sl-better-crafting/releases/tag/v1.0.0).
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\BetterCrafting.dll`

## Uninstall

Delete `BetterCrafting.dll` from the `BepInEx\plugins` folder. On the next game start, the workbench works as without the mod.

## Configuration

To change mod settings, edit `BepInEx\config\com.ivmakk.survivallog.bettercrafting.cfg` while the game is closed (see [CONFIG.md](CONFIG.md) for every setting). Changes apply at the next game start.

## Troubleshooting

If a game update removes a part of the workbench that the mod needs, Craft from any storage turns itself off, and the workbench works as without the mod.

Check `BepInEx\LogOutput.log` for the `Better Crafting loaded.` line. Check for warnings or errors from Better Crafting.

## Build

Better Crafting is a BepInEx 6 IL2CPP plugin. Building requires the .NET 8 SDK and a game installation with BepInEx. Start the game once after installing BepInEx to generate the IL2CPP interop assemblies. These assemblies come from the game and are not included in this repository.

```
dotnet build src/BetterCrafting.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam installation path. For another location, set the `GameDir` environment variable or pass `-p:GameDir=...` to the build command. The output DLL is `src\bin\Release\BetterCrafting.dll`.

Unit tests cover the list of places for the fill: the game's places first and in their order, then the storage of your home, each place once. The tested code has no game dependencies. It lives in `src/AnyStorage/AnyStorageLogic.cs`.

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
