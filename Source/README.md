# Repair Hammer

Each Repair Alloy Hammer Head Small consumes 15 Crystal Gem Blue (item hash 45754) and 5 Gold ingots in the vanilla Small Hammer Mould. Larger stacks are processed one recipe at a time. Crystal Gem Blue is explicitly accepted by the smelter input filter. Attach that head to a normal handle to use it as the repair hammer. Its registered Repair Alloy physical-material identity makes it a repair hammer after saving or reconnecting.

A Repair Alloy Hammer Head Small repairs a hot, completed, damaged forged item resting on an anvil once attached to a handle. Ordinary Crystal Gem Blue and Gold hammer heads do not repair items.

## Forge-based repair balance

Repair amount and repair-hammer damage are not hard-coded per metal. At server startup, the mod reads each ingot material that the game's forge moulds allow, reads its native `forgeMultiplier`, and calculates its profile across that live range:

- Higher forge multiplier (easier to forge) = more item repair and less repair-hammer damage.
- Lower forge multiplier (harder to forge) = less item repair and more repair-hammer damage.
- The easiest allowed material receives 50% repair / 2% hammer damage; the hardest receives 5% repair / 25% hammer damage. Materials in between are interpolated.

If a game update changes a forge multiplier, restart the server: the profiles will be recalculated automatically. The startup log records every detected material, multiplier, repair amount, and hammer-damage amount.

Current values reported by this game version at server startup:

| Target metal | Internal material | Repair | Repair-hammer damage |
| --- | --- | ---: | ---: |
| Copper | Copper | 50% | 2% |
| Gold | Gold | 40% | 7% |
| Iron | Iron | 33% | 11% |
| Orchi | Orchi Alloy | 33% | 11% |
| Red Iron | Red Iron Alloy | 33% | 11% |
| Carsi | Carsi Alloy | 26% | 14% |
| White Gold | White Gold Alloy | 26% | 14% |
| Silver | Silver | 24% | 15% |
| Mythril | Mythril | 8% | 23% |
| Evinon Steel | Evinon Steel Alloy | 5% | 25% |

## Installation

Build the mod with the game assemblies for the target A Township Tale version. Install the following on the server and on every client:

- `RepairHammer.dll`

`RepairHammer.dll` is self-contained: it includes the limited custom-recipe registration and smelter-filter behavior required for this mod. `CustomRecipesAPI.dll` is not required for Repair Hammer. Keep CustomRecipesAPI installed only if another mod on the same server or client needs it.

No Unity project, AssetBundle, custom mould, alloy ingot, model, texture, or colour asset is required. At startup the mod clones the vanilla Iron `PhysicalMaterial` and a compatible vanilla two-input/one-output `SmeltingRecipe`, assigns stable hashes, registers both through the normal game/API registries, and adds Crystal Gem Blue to the smelter's accepted-item filter. Do not create a new mould, alloy ingot, or general-purpose alloy recipe.

## Compatibility and verification

The recipe and the Repair Alloy material must be registered by the same game/mod version on the server and every connected client. Before release, verify the exact Small Hammer Mould, that the 15 Crystal Gem Blue / 5 Gold recipe processes larger stacks one item at a time, rejected ingredients and other moulds, ordinary Crystal Gem Blue/Gold hammer exclusion, a hot completed damaged target on an anvil, and persistence after a server/client reconnect.
