# Party Taxes v0.3

> *A game about paying your empl— ...friends, to adventure with you on a quest to slay pests for the queen.*

---------------------------------------------------------------------------------------

- **Engine:** Unity 6000.3.9f1
- **Input:** Unity Input System package
- **UI:** TextMesh Pro

---------------------------------------------------------------------------------------

## Controls

### Town
| Key | Action |
---------------------------------------------------------------------------------------
| `Space` | Sleep (advance day) |
| `E` | Go on an encounter |
| `F` | Recruit a new party member |
| `Q` | Heal the party (cleric) |
| `1` | Bless the party (cleric) |
| `3` | Resurrect a fallen member *(unlocks day 7)* |
| `Escape` | Reset / restart the game |

### Combat
---------------------------------------------------------------------------------------
| `Left Click` | Attack |
| `R` | Run away |

---------------------------------------------------------------------------------------

## Gameplay Systems

### Party Management
Recruit and manage a party of up to 4 adventurers. Each member has stats (HP, attack, defense), a level, and daily wages you must pay. New recruits are leveled up to match your party's average. Members who die are permanently tracked and can only return through resurrection — never normal recruitment.

### Gold & Economy
Gold is your core resource. You earn it by defeating enemies and through weekly survival bonuses. You spend it on wages, healing, blessings, recruitment, and resurrection. Party members may also steal gold while you sleep, with higher theft rates and amounts when wages go unpaid.

### Day / Night Cycle
Each day you can perform town actions and trigger one encounter. Sleeping advances the day, pays wages, rolls for theft, and resets all daily actions. Consecutive days without paying wages increase theft chance and inch towards mutiny.

### Combat
Encounters spawn a scaled group of enemies based on party size and days elapsed. Each party member attacks a random enemy, then surviving enemies counter-attack. The member who lands the killing blow earns all XP from that enemy. Defeating all enemies awards gold loot and a commission bonus.

### Healing & Blessings
The cleric can heal the party once per day for a small gold fee, restoring HP to each member. A full blessing costs significantly more but fully restores HP and removes the Cowardly debuff. Cowardly members receive 30% reduced healing from normal heals.

### Recruitment
Recruit a new party member for half your current total daily wages. Cowardly members inflate recruitment costs by 50%. When all named adventurers have died, a mysterious stranger with fully randomized stats can be recruited instead.

### Fleeing
Escape from battle at the cost of 10% of your current gold. All surviving (non-resurrected) party members are branded as Cowardly. No loot or commission is earned.

### Resurrection
Unlocked on day 7. Pay 100 gold plus the fallen member's daily wages to resurrect a random dead party member. Resurrected members return with a 10% stat penalty and are **Marked by Death** — they cannot become Cowardly but carry permanent stat reductions.

### Leveling
Members gain XP from kills. Every 2 levels grants +1 attack and +1 defense. Each level up also increases max HP by 10 and daily wages by 25%, so stronger parties cost more to maintain.


## Game Over Conditions

| Condition | Trigger |
---------------------------------------------------------------------------------------
| **Mutiny** | Wages unpaid for 3 consecutive days |
| **Total party wipe** | All party members killed in combat |

---------------------------------------------------------------------------------------

## Script Architecture

Contains a 4 part partial manager class and random recruit generator attached to a Manager GameObject, a CharacterData script called PTSoul

### `PTManager` — partial class, 4 files

The central game controller. Owns all shared state (gold, party list, enemy list, day counter, UI references, balance constants) and is split into partial files for readability:

| File | Responsibility |
---------------------------------------------------------------------------------------
| `PTManager.cs` | Variables, constants, `Awake`/`Start`, input setup, shared helpers (`ChangeGold`, `UpdateUI`, `TriggerGameOver`, `SetSunIntensity`) |
| `PTManager.Party.cs` | Party formation (`GetNextPartyMemberTransform`), `AddPartyMember`, `RemovePartyMember`, `LevelUpToPartyAverage`, `GetAveragePartyLevel` |
| `PTManager.Combat.cs` | `OnEncounter`, `OnAttack` (with kill-credit XP), `OnRunAway` |
| `PTManager.Town.cs` | `OnSleep`, `OnHeal`, `OnBless`, `OnRecruit`, `OnResurrect`, `OnReset`, `CanPerformTownAction` |

### `PTSoul`
Represents a single character — party member or enemy. Owns stats (HP, attack, defense, level, XP, wages) and status flags (`isCowardly`, `markedByDeath`, `isAdversary`). Handles its own damage, healing, blessing, XP, and level-up logic, and drives its own UI elements (name, health bar, XP bar). `PTManager` holds lists of `PTSoul` instances and calls into them to run gameplay.

### `PTRecruitGenerator`
Fallback recruit factory used when all named adventurer prefabs have been exhausted. Generates random names from configurable prefix/suffix pools, randomizes stats within inspector-set ranges, and scales those stats by level. Called by `PTManager.Town` during recruitment.

### `PTAdventureLog`
Singleton scrollable narrative log. Any script calls `PTAdventureLog.Log(message)` to append a rich-text entry. Handles auto-scrolling, entry count limits, and custom color tags for enemies (`<enemy>`), party names (`<partyname>`), etc.

### `DeadCharacterData`
Serializable snapshot class defined alongside `PTManager`. Captures a fallen `PTSoul`'s name, stats, and original prefab reference at the moment of death. Used exclusively by the resurrection system to restore the character with a configurable stat penalty.

---------------------------------------------------------------------------------------

## Extra stuff

- **Debug Mode:** If enabled, prints extra debug messages to the console.
- **Gold never goes negative:** All deductions clamp at zero.
- **Resurrection prefab fallback:** If the original prefab for a dead character cannot be found, the first prefab in the array is used.
- **Dynamic UI controls:** The controls display updates based on context (town/combat).
- **Sun intensity changes:** The sun's intensity changes when sleeping or after combat.
- **Party formation:** Members are repositioned and rotated to maintain formation when new members are added.
- **Randomized sleep messages:** Sleep messages are randomized each night.
- **Weekly bonus:** Every 7 days, a gold bonus is awarded.
- **Adventure log formatting:** All major actions and events are logged with rich text formatting, including color tags.
- **Cowardly status:** Resurrected members cannot become Cowardly; normal members can after fleeing or healing/blessing.
- **Recruitment cost:** Increased by 50% if any party member is Cowardly.
- **Healing/Blessing:** Only available once per day each; Cowardly members get 30% reduced healing.
- **Level up to party average:** New recruits are leveled up to match the average level of the existing party (excluding themselves).
- **Game over triggers:** Mutiny after 3 consecutive unpaid days; total party wipe triggers game over immediately.
- **Audio feedback:** Gold changes play a sound effect if assigned.
- **DeadCharacterData:** Stores full stat snapshot for resurrection, including prefab reference and fallback.
