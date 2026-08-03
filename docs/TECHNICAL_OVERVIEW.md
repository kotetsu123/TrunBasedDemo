# Adventure of Paul Demo Technical Overview

## 1. Document Purpose

This document summarizes the current technical structure of `Adventure of Paul Demo`.

The project is a Unity turn-based RPG systems demo. The goal is not to ship a full game yet, but to connect common RPG systems into a playable and extensible loop:

```text
Field exploration
 -> Field encounter / interactable
 -> Battle scene
 -> Reward / result flow
 -> Runtime state writeback
 -> Save / Load
 -> Return to Field
```

中文概括：

```text
这是一个以作品集为目标的 Unity 回合制 RPG demo。
当前重点是把 Field、Battle、Inventory、Party、Reward、Save/Load、Dialogue、Tutorial 等系统串成可体验流程。
```

## 2. High-Level Architecture

| System | Responsibility | Main Scripts |
| --- | --- | --- |
| Field System | Field scene bootstrapping, player placement, generated spawn/interactable roots | `FieldCreator`, `FieldData`, `FieldSaveContext` |
| Encounter System | Field enemy spawn, trigger, respawn, group encounter | `EnemySpawnManager`, `EnemySpawnPoint`, `EnemyFieldController`, `EncounterTrigger`, `FieldBattleContext` |
| Battle System | Turn flow, timeline, target selection, battle camera, result handling | `BattleManager`, `BattleSpawner`, `BattleFormation`, `BattleTargetSelector`, `BattleCameraDirector` |
| Runtime State | Cross-scene party, inventory, tutorial state | `PartyRuntimeState`, `InventoryRuntimeState`, `TutorialRuntimeState` |
| Data Tables | ScriptableObject gameplay configuration | `FieldData`, `EncounterData`, `EncounterDataBase`, `EnemyCharacterDataBase`, `ItemDataBase`, `CharacterDataBase` |
| Reward System | EXP, item drops, reward result payload | `EncounterRewardService`, `EncounterRewardResult`, `RewardPopController` |
| Field Interactables | Chest, recruit point, dialogue trigger, E prompt | `FieldChestController`, `FieldRecruitController`, `FieldDialogueController`, `FieldInteractionPromptController` |
| Save / Load | JSON save data and runtime restoration | `SaveSystem`, `GameSaveData`, `FieldSaveData`, `InventorySaveData`, `PartyMemberSaveData`, `TutorialSaveData` |
| UI System | Battle UI, field inventory, result, reward, tutorial, dialogue | `BasePanel`, `DialoguePanelController`, `TutorialPanelController`, `RewardPopController`, `LevelUpPopController` |

Core runtime data flow:

```text
FieldCreator
 -> FieldData
 -> EnemySpawnManager / Field Interactables
 -> EncounterTrigger
 -> FieldBattleContext
 -> BattleSpawner
 -> BattleManager
 -> EncounterRewardService
 -> PartyRuntimeState / InventoryRuntimeState
 -> SaveSystem
 -> FieldCreator
```

## 3. Field Scene Structure

The current 3D field scene is treated as a hand-authored small world. Large visual and collision work stays in the Unity scene, while gameplay points can be driven by data.

Recommended hierarchy:

```text
FieldScene
├─ Environment
│  ├─ Ground
│  ├─ Walls
│  └─ Props
├─ SpawnPoints
│  ├─ EnemySpawnPoints
│  ├─ ChestSpawnPoints
│  ├─ RecruitSpawnPoints
│  └─ DemoRouteMarkers
├─ RuntimeGenerated
│  ├─ GeneratedSpawnPoints
│  ├─ GeneratedInteractables
│  └─ GeneratedEnvironment
└─ Managers
   ├─ FieldCreator
   ├─ EnemySpawnManager
   └─ SceneTransitionController
```

Rule of thumb:

- `Environment/Props`: static decoration, no save id, no gameplay state.
- `SpawnPoints`: designer-placed gameplay markers.
- `RuntimeGenerated`: objects created by `FieldCreator` or spawn managers.

## 4. FieldCreator

File:

```text
Assets/Scripts/Filed/FieldCreator.cs
```

`FieldCreator` is the Field scene boot entry.

Current startup order:

```text
FieldPauseState.Clear()
 -> PartyRuntimeState.InitializeIfEmpty()
 -> InventoryRuntimeState.InitializeIfEmpty()
 -> CreateSpawnPointsFromFieldData()
 -> CreateObjectsFromFieldData()
 -> CreateRecruitPointsFromFieldData()
 -> SetupPlayer()
 -> EnemySpawnManager.SpawnAll()
 -> FieldPartyHudController.Refresh()
 -> Battle return cooldown
```

Player transform priority:

```text
1. Battle return position
2. Saved player transform from Load
3. Scene playerStartPoint
```

Generated root behavior:

- Enemy spawn points are parented under `generatedSpawnPointRoot`.
- Chests and recruit points are parented under `generatedInteractableRoot`.
- Non-interactable field objects are parented under `generatedEnvironmentRoot`.
- If a root is missing, the code falls back to a nearby root or the `FieldCreator` transform.

## 5. FieldData

File:

```text
Assets/Scripts/Filed/FieldData.cs
```

In the current 3D demo, `FieldData` is a gameplay table, not a full map generator.

It currently contains:

```text
FieldData
├─ fieldId
├─ spawnPoints
├─ fieldObjects
└─ recruitPoints
```

### 5.1 Spawn Points

`FieldSpawnPointEntry` describes enemy encounter points:

```text
spawnId
encounterId
fieldPrefab
position
rotationEuler
wanderRadius
enemyId
respawnType
respawnSeconds
```

Recommended new flow:

```text
FieldSpawnPointEntry.encounterId
 -> EncounterDataBase
 -> EncounterData.enemyEntries
 -> EnemyCharacterDataBase
 -> Battle enemies
```

Legacy compatibility:

- `enemyId` can still be used with `EnemyDataBase` to fill old field enemy settings.
- New content should prefer `encounterId`.

### 5.2 Field Objects

`FieldObjectEntry` describes generated field objects:

```text
objectId
prefab
position
rotationEuler
scale
```

Current supported object:

```text
FieldChestController
```

`objectId` becomes the chest save id when the generated prefab has `FieldChestController`.

### 5.3 Recruit Points

`FieldRecruitPointEntry` describes data-driven recruit points:

```text
recruitId
characterId
pointPrefab
visualPrefab
position
rotationEuler
scale
interactPrompt
preRecruitDialogue
disableAfterRecruit
```

Recommended setup:

- `pointPrefab`: logic object with `FieldRecruitController` and trigger collider.
- `visualPrefab`: model / animation only.
- The generated visual is placed under the recruit point.
- Nested `FieldRecruitController` and colliders inside the visual are disabled by `FieldCreator` to avoid duplicate interactions.

## 6. Field Encounter And Respawn

### 6.1 EnemySpawnManager

File:

```text
Assets/Scripts/Filed/EnemySpawn/EnemySpawnManager.cs
```

Responsibilities:

- Spawn enemies from scene or data-generated `EnemySpawnPoint`.
- Keep `activeEnemiesBySpawnId` so one spawn point does not create duplicate active enemies.
- Check `FieldBattleContext.ShouldSkipSpawn()` before spawning.
- Support live timed respawn while the player stays in the same Field scene.

### 6.2 FieldBattleContext

File:

```text
Assets/Scripts/Filed/FieldBattleContext.cs
```

This is the static bridge between Field and Battle.

It stores:

- Last Field scene name.
- Player position and rotation before battle.
- Current `spawnId`.
- Current `encounterId`.
- Encounter cooldown after returning from battle.
- Cleared spawn ids.
- Cleared spawn UTC timestamps for timed respawn.
- Opened chest ids.
- Saved player transform from Load.

Respawn logic:

```text
spawnId not cleared
 -> spawn allowed

spawnId cleared + Permanent
 -> skip spawn

spawnId cleared + Timed + elapsed time < respawnSeconds
 -> skip spawn

spawnId cleared + Timed + elapsed time >= respawnSeconds
 -> remove cleared record and spawn again
```

### 6.3 EncounterTrigger

File:

```text
Assets/Scripts/Filed/Encounter/EncounterTrigger.cs
```

The trigger starts a battle when the player touches a field enemy.

It also supports group encounter visualization:

- Nearby enemies can be detected for group encounter.
- Runtime `LineRenderer` links can show which enemies will join.
- Link targets are cached and updated in `LateUpdate` to reduce visual jitter.

## 7. Battle System

### 7.1 BattleSpawner

File:

```text
Assets/Scripts/Battle/BattleSpawner.cs
```

Player spawning:

```text
PartyRuntimeState.PartyMembers
 -> clone Character data
 -> instantiate player controller
 -> BattleFormation player slots
```

Enemy spawning:

```text
FieldBattleContext.CurrentEncounterId
 -> EncounterDataBase.FindById()
 -> EncounterData.enemyEntries
 -> EnemyCharacterDataBase.FindByEnemyId()
 -> clone Character template
 -> instantiate enemy controller
 -> BattleFormation enemy slots
```

Fallback behavior:

- If encounter data is missing or invalid, `initialEnemies` can still be used for testing.
- `allowLegacyEnemyCharactersFallback` controls whether old serialized enemy character lists may be used.

### 7.2 BattleManager

File:

```text
Assets/Scripts/Battle/BattleManager.cs
```

Main responsibilities:

- Register battle actors.
- Advance action values and timeline order.
- Manage current actor, selected target, and preview target.
- Execute attack / skill / item commands.
- Handle death, revive, and timeline icon rebuild.
- Resolve battle win / lose / escape.
- Build result payload for result UI.
- Show skill and battle event popup messages.

Important rules:

- Player death does not remove the player controller from the battle controller list.
- Dead players stay in runtime data so Field HUD and revive items still work correctly.
- Enemy death can remove enemy controllers and free formation slots.
- Action confirmation is blocked while `BattleCameraDirector.IsMoving` is true.
- Target switching can remain responsive while the camera is moving.

### 7.3 Timeline

The timeline displays the upcoming actor order based on action values and speed.

Recent behavior:

- UI order is updated from the same prediction logic used by battle turn selection.
- This prevents the UI from showing one expected actor while the system resolves another.

### 7.4 Battle Camera

File:

```text
Assets/Scripts/Battle/BattleCameraDirector.cs
```

Battle intro camera behavior is driven by `EncounterData`.

```text
EncounterData.introCameraType
 -> BattleSpawner.GetIntroCameraType()
 -> BattleManager
 -> BattleCameraDirector
```

Current types:

- `Normal`: skips the heavy boss-style intro and uses a player-side target preview shot.
- `Boss`: uses the existing cinematic intro sequence.

Normal intro currently focuses on:

```text
next friendly actor + first alive enemy target
```

This gives regular encounters a faster start while preserving boss presentation.

## 8. Encounter And Reward Data

### 8.1 EncounterData

File:

```text
Assets/Scripts/Filed/Encounter/EncounterData.cs
```

Current fields:

```text
encounterId
enemyEntries
legacy enemy characters
rewardExp
itemDrops
introCameraType
```

Recommended enemy table setup:

```text
EncounterData.enemyEntries:
    enemyId: slime
    count: 3

EnemyCharacterDataBase:
    Character.characterId: slime
```

`EncounterEnemyEntry.enemyId` currently maps to `Character.characterId` inside `EnemyCharacterDataBase`.

### 8.2 Reward Flow

Reward resolution:

```text
BattleManager handles Win
 -> EncounterRewardService.GrantRewards()
 -> add EXP to battle player controllers
 -> roll item drops
 -> InventoryRuntimeState.AddItem()
 -> EncounterRewardResult
 -> RewardPopController
 -> LevelUpPopController
```

Drop rule:

```text
Random.value <= dropChance
 -> drop succeeds
 -> Random.Range(minCount, maxCount + 1)
 -> item added to inventory
```

If no item drops, reward UI still shows the EXP result so the player receives visible feedback.

## 9. Inventory And Item System

### 9.1 InventoryRuntimeState

File:

```text
Assets/Scripts/Filed/Inventory/InventoryRuntimeState.cs
```

Inventory is stored as ordered slots.

Reason:

- UI slot order must stay stable.
- Drag and drop needs slot indexes.
- Save / Load needs to restore item positions.

Current abilities:

- Initialize fixed default capacity.
- Keep empty slots.
- Stack same item data.
- Consume item count.
- Swap slots.
- Serialize to `InventorySaveData`.
- Load from `InventorySaveData` using `ItemDataBase`.

### 9.2 Item Types

File:

```text
Assets/Scripts/Enums/ItemType.cs
```

Current item types:

```text
None = 0
Heal = 1
RestoreMp = 2
Revive = 3
Buff = 4
```

Explicit enum values are used because Unity serializes enum values as numbers. This prevents old item assets from changing meaning when new enum entries are added.

Implemented:

- HP recovery.
- MP recovery.
- Revive.

Reserved:

- Buff item data fields exist, but buff runtime behavior is not implemented yet.

## 10. Party And Recruit System

### 10.1 PartyRuntimeState

File:

```text
Assets/Scripts/Data/RunTime/PartyRuntimeState.cs
```

Responsibilities:

- Initialize party from `PartyInitialData`.
- Save / Load party members.
- Write battle results back into runtime data.
- Preserve party order.
- Recruit new members.
- Keep dead members visible for Field revive flow.

Writeback matching:

```text
characterId first
 -> fallback by Name for older data
 -> append new members
```

### 10.2 FieldRecruitController

File:

```text
Assets/Scripts/Filed/FieldObjects/FieldRecruitController.cs
```

Recruit flow:

```text
Player enters trigger
 -> show E prompt
 -> optional preRecruitDialogue
 -> CharacterDataBase.FindById(characterId)
 -> PartyRuntimeState.TryRecruitMember()
 -> refresh FieldPartyHud
 -> hide visualRoot if disableAfterRecruit
```

Load rollback behavior:

- `SaveSystem.Load()` calls `FieldRecruitController.RefreshAllRecruitStates()`.
- If a loaded save no longer has the recruited member, the recruit point becomes visible again.
- The logic point should stay active; only `visualRoot` should be hidden after recruit.

## 11. Dialogue And Tutorial

### 11.1 Dialogue

Files:

```text
Assets/Scripts/Data/DialogueData/DialogueData.cs
Assets/Scripts/Filed/FieldObjects/FieldDialogueController.cs
Assets/Scripts/UI/DialoguePanelController.cs
```

Dialogue flow:

```text
FieldDialogueController
 -> player enters trigger
 -> E
 -> DialoguePanelController.Play(dialogueData)
 -> click / input advances line
 -> callback on complete
```

Recruit points can optionally play dialogue before adding the party member.

### 11.2 Tutorial

Files:

```text
Assets/Scripts/Data/TutorialData/TutorialData.cs
Assets/Scripts/Data/RunTime/TutorialRuntimeState.cs
Assets/Scripts/Data/SaveData/TutorialSaveData.cs
Assets/Scripts/Filed/FieldTutorialController.cs
Assets/Scripts/Battle/BattleTutorialController.cs
Assets/Scripts/UI/TutorialPanelController.cs
```

Tutorial flow:

```text
TutorialController
 -> TutorialRuntimeState.IsCompleted(tutorialId)
 -> TutorialPanelController.Play(tutorialData)
 -> player reads pages
 -> Skip can ask for confirmation
 -> TutorialRuntimeState.MarkCompleted(tutorialId)
 -> SaveSystem writes TutorialSaveData
```

Current tutorial support:

- Field tutorial entry.
- Battle tutorial entry.
- Multi-step tutorial data.
- Skip confirmation.
- Save / Load completed tutorial ids.

## 12. Save / Load System

File:

```text
Assets/Scripts/Manager/SaveSystem.cs
```

Save file:

```text
Application.persistentDataPath/save.json
```

Save flow:

```text
SaveSystem.Save()
 -> BuildSaveData()
 -> InventoryRuntimeState.ToSaveData()
 -> PartyRuntimeState.ToSaveData()
 -> FieldBattleContext.ToSaveData()
 -> FieldSaveContext.TryFillFieldSaveData()
 -> TutorialRuntimeState.ToSaveData()
 -> JsonUtility.ToJson()
 -> File.WriteAllText()
```

Load flow:

```text
SaveSystem.Load(itemDataBase, characterDataBase)
 -> File.ReadAllText()
 -> JsonUtility.FromJson<GameSaveData>()
 -> InventoryRuntimeState.LoadFromSaveData()
 -> PartyRuntimeState.LoadFromSaveData()
 -> FieldBattleContext.LoadFromSaveData()
 -> TutorialRuntimeState.LoadFromSaveData()
 -> FieldSaveContext.TryApplySavedPlayerTransform()
 -> FieldRecruitController.RefreshAllRecruitStates()
```

Saved data:

```text
GameSaveData
├─ version
├─ inventory
├─ party
├─ field
└─ tutorial
```

Stable id restoration:

```text
itemId -> ItemDataBase -> ItemData
characterId -> CharacterDataBase -> Character
spawnId -> FieldBattleContext cleared spawn state
chestId -> FieldBattleContext opened chest state
tutorialId -> TutorialRuntimeState completed state
```

## 13. UI System

### 13.1 BasePanel

File:

```text
Assets/Scripts/UI/BasePanel.cs
```

`BasePanel` centralizes UI show/hide behavior through `CanvasGroup`:

- `alpha`
- `interactable`
- `blocksRaycasts`

### 13.2 Field UI

Current Field UI:

- Field party HP HUD.
- Inventory panel.
- Inventory description panel.
- Party target panel for item usage.
- ESC menu for save/load.
- Interaction prompt for E interactions.
- Dialogue panel.
- Tutorial panel.

### 13.3 Battle UI

Current Battle UI:

- Command panel.
- Skill panel.
- Item panel.
- Timeline UI.
- Skill / battle event popup.
- Reward popup.
- Level-up popup.
- Result / settle panel.

Popup semantics:

- `ShowSkillName()` is for skill or action names.
- `ShowBattleEventPopup()` is for event messages such as group encounter.
- `SkillNamePopController` keeps its old class name to preserve Unity Inspector bindings, but now acts as a shared battle popup component.

## 14. Demo Route

The current demo is moving toward a simple route:

```text
Start
 -> Slime
 -> Chest
 -> Recruit Argo
 -> Group Encounter
 -> Boss
```

Purpose:

- Slime: regular timed respawn enemy.
- Chest: field interactable, item reward, opened state save.
- Recruit Argo: party member join flow and load rollback test.
- Group Encounter: nearby enemy group detection and visible link lines.
- Boss: permanent clear, boss intro camera, stronger reward.

This route is meant to prove that the systems are connected, not to be final level art.

## 15. Compatibility And Current Boundaries

Compatibility:

- FieldData can be missing; old scene-placed spawn points still work.
- EncounterData prefers `enemyEntries`, but legacy enemy character fallback can still be enabled.
- Save data uses stable ids instead of Unity object references.
- Old cleared spawn ids can be restored even without timed respawn records.

Current boundaries:

- FieldData is not a full 3D world generator.
- Buff item type is reserved but not implemented.
- Equipment system is planned but not implemented.
- Dialogue and recruit are connected, but a full quest/event system does not exist yet.
- Tutorial exists, but more content needs to be authored.
- Demo scene layout is still graybox / route planning stage.
- `EnemyFieldData` remains mainly for older field enemy compatibility.

## 16. Recommended Next Steps

Short-term portfolio tasks:

1. Finish the demo route layout and make the path readable.
2. Author battle tutorial content for attack, skill, item, run, and target selection.
3. Add a simple ending point after the boss.
4. Polish reward popup text and result flow.
5. Add screenshots or diagrams to README.

Medium-term system tasks:

1. Convert field enemy behavior from coroutine-driven logic to a state machine.
2. Add a first version of equipment data, equipment slots, and stat modifiers.
3. Expand interactable data for portal / event / quest objects.
4. Add save migration handling using `GameSaveData.version`.
5. Clean up legacy encounter fields after all encounters use `enemyEntries`.

## 17. Portfolio Summary

This project demonstrates:

- ScriptableObject data-driven RPG configuration.
- Cross-scene Field and Battle state handoff.
- Runtime party and inventory persistence.
- Turn-based battle timeline and target selection.
- Encounter table based enemy generation.
- Reward service separation.
- JSON save/load DTO design.
- Permanent and timed respawn.
- Chest, recruit, dialogue, and tutorial interaction flows.
- Practical compatibility handling while migrating from scene-authored data to table-driven gameplay.

One-sentence English summary:

```text
Adventure of Paul Demo keeps heavy 3D scene authoring inside Unity while making RPG gameplay entities, encounters, rewards, runtime state, tutorials, and save data configurable through reusable data-driven systems.
```
