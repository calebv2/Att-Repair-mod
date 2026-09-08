# Crystal Repair Hammer

The mod is named **Crystal Repair Hammer**. Its DLL filename is `CrystalRepairHammer.dll`.

Each Crystal Repair Alloy Hammer Head Small consumes 15 Crystal Gem Blue and 5 Gold ingots in the vanilla Small Hammer Mould. Larger stacks are processed one recipe at a time.

Crystal Gem Blue is explicitly accepted by the smelter input filter. Attach that head to a normal handle to use it as the Crystal Repair Hammer. Its registered Repair Alloy physical-material identity makes it a repair hammer after saving or reconnecting.

A Crystal Repair Alloy Hammer Head Small repairs a hot, completed, damaged forged item resting on an anvil.

Ordinary Crystal Gem Blue and Gold hammer heads do not repair items.

## Forge-based repair balance

At server startup, the mod reads each ingot material that the game's forge moulds allow, reads its native `forgeMultiplier`, and calculates its profile across that live range:

- Higher forge multiplier (easier to forge) = more item repair and less repair-hammer damage.
- Lower forge multiplier (harder to forge) = less item repair and more repair-hammer damage.
- The easiest allowed material receives 50% repair / 2% hammer damage; the hardest receives 5% repair / 25% hammer damage. Materials in between are interpolated.

Current values reported by default:

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

Install the following on the server and on every client: CrystalRepairHammer.dll
