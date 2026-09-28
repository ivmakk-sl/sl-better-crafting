# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-09-28

### Added

- Craft from any storage: a click on a recipe fills the workbench grid with the missing materials from all storage of your home, not only from the backpack, the Workbench Drawer, and the Tool Cabinets. The home is the same as for the seeds of the planting window: the home floor, and each floor and area of the home that is unlocked.
- A fixed pick order: the backpack first, then the Workbench Drawer and the Tool Cabinets, then the other storage. In each place, clean items before polluted items, and the items that expire first before fresher items.
- No partial fill: when the home does not have enough of each material, nothing moves, and the game shows its usual message.
- The "have / need" counts of the recipe list and the brown border of a recipe with enough materials include the storage of your home.
- Craft Again fills the grid from the same places.
- Craft from any storage turns itself off when a mod that also changes the workbench fill is installed. The README names it under Compatibility.
