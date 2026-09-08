# Crystal Repair Hammer

The mod is named **Crystal Repair Hammer**. Its DLL filename is `CrystalRepairHammer.dll`.

Each Crystal Repair Alloy Hammer Head Small consumes 15 Crystal Gem Blue and 5 Gold ingots in the vanilla Small Hammer Mould. Larger stacks are processed one recipe at a time.

Crystal Gem Blue is explicitly accepted by the smelter input filter. Attach that head to a normal handle to use it as the Crystal Repair Hammer. Its registered Repair Alloy physical-material identity makes it a repair hammer after saving or reconnecting.

A Crystal Repair Alloy Hammer Head Small repairs a hot, completed, damaged forged item resting on an anvil.

Ordinary Crystal Gem Blue and Gold hammer heads do not repair items.

## Appearance

The Crystal Repair Alloy head uses the game's heated-metal material effect at full intensity, giving it a permanent heated appearance without changing its actual temperature, melting behavior, or repair gameplay. This visual effect is client-side; players need the current `CrystalRepairHammer.dll` to see it.

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

- `CrystalRepairHammer.dll`

`CrystalRepairHammer.dll` is self-contained: it includes the limited custom-recipe registration and smelter-filter behavior required for this mod. `CustomRecipesAPI.dll` is not required for Crystal Repair Hammer. Keep CustomRecipesAPI installed only if another mod on the same server or client needs it.
