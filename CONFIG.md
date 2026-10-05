# Configuration

Every setting lives in `BepInEx\config\com.ivmakk.survivallog.bettercrafting.cfg`, written the first time you run the game with the mod installed. Edit it with any text editor while the game is closed. Changes apply at the next game start.

## General

| Setting | Default | Values | What it does |
|---|---|---|---|
| `Verbose` | `false` | `true` / `false` | Writes extra diagnostic lines to the BepInEx log for troubleshooting. Keep off in normal play. |

## Performance

| Setting | Default | Values | What it does |
|---|---|---|---|
| `CountCache` | `true` | `true` / `false` | Counts the materials of the workbench places once for each update of the recipe list, so crafting stays fast with much storage in your home. Set it to `false` only if a "have / need" count of the recipe list looks wrong: the counts then work as in version 1.1.0, and crafting is slow again with much home storage. |
