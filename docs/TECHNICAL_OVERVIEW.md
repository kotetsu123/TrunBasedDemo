# Adventure of Paul Demo Technical Overview

## 1. Document Purpose

This document summarizes the current technical structure of `Adventure of Paul Demo` as of Demo v1.

The project is a Unity turn-based RPG systems demo. Demo v1 focuses on proving that the major RPG systems can be connected into a playable and extensible loop:

```text
Field exploration
 -> Field encounter / interactable
 -> Battle scene
 -> Reward / result flow
 -> Runtime state writeback
 -> Save / Load
 -> Return to Field
 -> Boss Field
 -> Boss battle
 -> Ending dialogue
 -> Demo clear panel
```

中文概括：

```text
这是一个以作品集为目标的 Unity 回合制 RPG demo。
Demo v1 已经完成从 Field 探索、普通战斗、Boss 战、Ending Dialogue 到 DemoEndPanel 的基础闭环。
当前重点从“继续扩系统”转向“打磨体验、补测试、补内容和文档”。
```

## 1.1 Demo v1 Closed Loop

Current verified high-level play flow:

```text
Title
 -> Field maze
 -> Chest / recruit / regular encounter / group encounter
 -> Boss Field transition
 -> Boss encounter
 -> Battle scene
 -> Return to Boss Field
 -> Auto ending dialogue
 -> DemoEndPanel
 -> Back to Title
```

中文：

```text
目前 Demo v1 的目标不是完整游戏，而是一个能从头走到尾的灰盒流程。
核心闭环已经可以跑通：Field 迷宫 -> Boss Field -> Boss 战 -> 结尾剧情 -> Demo 通关面板。
```

## 2. High-Level Architecture

| System | Responsibility | Main Scripts |
| --- | --- | --- |
| Field System | Field scene bootstrapping, player placement, generated spawn/interactable roots | `FieldCreator`, `FieldData`, `FieldSaveContext` |
| Encounter System | Field enemy spawn, trigger, respawn, group encounter | `EnemySpawnManager`, `EnemySpawnPoint`, `EnemyFieldController`, `EncounterTrigger`, `FieldBattleContext` |
| Battle System | Turn flow, timeline, target selection, battle camera, result handling | `BattleManager`, `BattleSpawner`, `BattleFormation`, `BattleTargetSelector`, `BattleCameraDirector` |
| Runtime State | Cross-scene party, inventory, tutorial, auto event state | `PartyRuntimeState`, `InventoryRuntimeState`, `TutorialRuntimeState`, `FieldAutoEventRuntimeState` |
| Data Tables | ScriptableObject gameplay configuration | `FieldData`, `EncounterData`, `EncounterDataBase`, `EnemyCharacterDataBase`, `ItemDataBase`, `CharacterDataBase` |
| Reward System | EXP, item drops, reward result payload | `EncounterRewardService`, `EncounterRewardResult`, `RewardPopController` |
| Field Interactables | Chest, recruit point, dialogue trigger, E prompt | `FieldChestController`, `FieldRecruitController`, `FieldDialogueController`, `FieldInteractionPromptController` |
| Save / Load | JSON save data and runtime restoration | `SaveSystem`, `GameSaveData`, `FieldSaveData`, `InventorySaveData`, `PartyMemberSaveData`, `TutorialSaveData`, `FieldAutoEventSaveData` |
| UI System | Battle UI, field inventory, result, reward, tutorial, dialogue, demo clear | `BasePanel`, `DialoguePanelController`, `TutorialPanelController`, `RewardPopController`, `LevelUpPopController`, `DemoEndController` |
| Scene Transition | Field-to-field transition and target spawn placement | `FieldSceneTransitionTrigger`, `SceneTransitionController`, `FieldSceneTransitionContext` |

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
 -> FieldAutoDialogueEventController
 -> DemoEndController
```

中文概括：

这一章说明项目的整体分层。当前核心链路是：Field 生成玩法点，玩家触发 Encounter，Battle 根据 EncounterData 生成敌人，战斗结束后奖励和状态回写，再由 SaveSystem 保存或读取。Demo v1 还额外接上了 Boss Ending 和 DemoEndPanel，让流程有明确结束点。

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

中文概括：

Field 场景现在是“手摆场景 + 数据驱动玩法点”的混合结构。墙、地面、装饰物继续放在 Unity 场景里手动摆；敌人生成点、宝箱点、入队点这类需要 ID、需要保存状态的东西，才适合交给 `FieldData` 和 `FieldCreator` 管理。

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

中文概括：

`FieldCreator` 是 Field 场景启动时的入口。它负责初始化运行时数据、根据 `FieldData` 生成敌人/宝箱/入队点、放置玩家、刷新 HUD，并在从战斗或其他 Field 场景返回时处理玩家落点。

## 4.1 Field Scene Transition

Files:

```text
Assets/Scripts/Filed/FieldSceneTransitionContext.cs
Assets/Scripts/Filed/FieldObjects/FieldSceneTransitionTrigger.cs
Assets/Scripts/Filed/SceneTransitionController.cs
```

Field-to-field transition flow:

```text
Player enters FieldSceneTransitionTrigger
 -> validate targetSceneName
 -> FieldSceneTransitionContext.SetPendingSpawnPoint(targetSpawnPointId)
 -> SceneTransitionController.StartSceneTransition(targetSceneName)
 -> fade canvas blocks raycasts
 -> FieldPauseState.SetPaused(true)
 -> SceneManager.LoadScene(targetSceneName)
 -> FieldCreator consumes pending spawn point
 -> player is placed at the target spawn point
```

Purpose:

- Move from the maze Field scene to `Boss_FieldScene`.
- Move back from Boss Field to the maze if needed.
- Keep scene transition placement data separate from battle return placement.

中文：

- `targetSceneName` 决定要切到哪个 Field 场景。
- `targetSpawnPointId` 决定进入新场景后玩家落在哪个点。
- `FieldSceneTransitionContext` 只保存一次临时传送点，`FieldCreator` 用完后会消费掉。
- Fade 期间会暂停 Field，避免敌人和玩家在画面变黑时继续移动或追击。

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

中文概括：

`FieldData` 不是完整地图生成器，而是场景玩法配置表。它负责描述敌人生成点、宝箱/物件、入队点等“可交互或会影响流程的对象”。3D 地形和装饰仍然建议手摆，避免为了数据化而把场景制作复杂化。

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

中文概括：

Field 遭遇系统负责从生成点生成敌人、判断是否应该刷新、玩家碰到敌人后进入战斗。`FieldBattleContext` 记录当前遭遇、战斗前玩家位置、已清除的 spawnId、宝箱开启状态等跨场景数据。联合遇敌会检测附近敌人，并用运行时线条提示哪些敌人会一起进入战斗。

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

中文概括：

Battle 系统现在已经支持从 `EncounterData` 生成敌人、行动条排序、目标选择、道具、复活、奖励结算和结果面板。普通战斗和 Boss 战可以使用不同的开场镜头配置。为了避免镜头移动时输入导致状态错乱，行动确认会在 camera moving 时被阻止。

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

中文概括：

`EncounterData` 决定一场战斗会生成哪些敌人，以及胜利后给多少经验、可能掉落哪些道具。奖励发放已经从 `BattleManager` 中拆到 `EncounterRewardService`，这样战斗流程和奖励计算不会完全混在一起。

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

中文概括：

背包现在是“固定顺序 slot”结构，而不是简单 Dictionary。这样可以保存道具所在格子，也能支持拖拽交换。道具类型目前支持 HP 回复、MP 回复、复活；Buff 类型已经预留，但实际 buff 效果还没接。

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

中文概括：

队伍数据的主来源是 `PartyRuntimeState`。入队点通过 `characterId` 从 `CharacterDataBase` 找角色，再加入队伍。读档时会根据当前队伍状态刷新入队点，如果存档里还没有 Argo，那么 Argo 的入队点应该重新显示出来。

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

### 11.2 Auto Dialogue Events

Files:

```text
Assets/Scripts/Filed/FieldObjects/FieldAutoDialogueEventController.cs
Assets/Scripts/Filed/FieldAutoEventRuntimeState.cs
```

Auto dialogue events are scene-start checks for story or route events.

Current boss ending flow:

```text
Boss battle victory
 -> FieldBattleContext marks boss_spawn_001 as cleared
 -> return to Boss_FieldScene
 -> FieldAutoDialogueEventController.Start()
 -> requiredClearedSpawnId check passes
 -> DialoguePanelController.Play(endingDialogue)
 -> FieldAutoEventRuntimeState.MarkCompleted(eventId)
 -> onDialogueFinished.Invoke()
 -> DemoEndController.ShowDemoEnd()
```

Key fields:

```text
eventId
requiredClearedSpawnId
playOnce
playDelaySeconds
dialogueData
dialoguePanel
onDialogueFinished
```

Design notes:

- `requiredClearedSpawnId` is used so the ending dialogue only starts after the boss is defeated.
- `eventId` is saved through `FieldAutoEventRuntimeState`, preventing completed auto events from replaying after Load.
- `onDialogueFinished` is a UnityEvent so the event controller does not need hard-coded knowledge of Demo End, reward, quest, or scene transition behavior.

中文：

- 自动剧情事件不是玩家按 E 触发，而是场景启动后自己检查条件。
- Boss 结尾使用 `boss_spawn_001` 作为条件，因为 Boss 胜利后这个 spawnId 会被记录为 cleared。
- 剧情播完后不靠字符串名字判断，而是通过 Inspector 里的 `onDialogueFinished` 接后续动作。
- 目前 Boss ending 的后续动作是打开 `DemoEndPanel`。

### 11.3 Tutorial

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

中文概括：

对话系统用于普通 Field 对话、入队前对话和 Boss 结尾剧情。Tutorial 系统和 Dialogue 分开，主要负责多页教程、跳过确认、完成状态保存。自动剧情事件则是场景启动后检查条件，适合 Boss 战后自动播放 ending。

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
 -> FieldAutoEventRuntimeState.ToSaveData()
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
 -> FieldAutoEventRuntimeState.LoadFromSaveData()
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
├─ tutorial
└─ fieldAutoEvents
```

Stable id restoration:

```text
itemId -> ItemDataBase -> ItemData
characterId -> CharacterDataBase -> Character
spawnId -> FieldBattleContext cleared spawn state
chestId -> FieldBattleContext opened chest state
tutorialId -> TutorialRuntimeState completed state
eventId -> FieldAutoEventRuntimeState completed state
```

Auto event save behavior:

```text
FieldAutoEventRuntimeState
 -> runtime HashSet<string> completedEventIds
 -> FieldAutoEventSaveData
 -> serializable List<string> completedEventIds
```

Reason:

- Runtime uses `HashSet` for quick duplicate checks.
- Unity `JsonUtility` serializes the save DTO list.

中文概括：

存档系统把运行时数据转成 DTO，再写进 `save.json`。读取时通过各种 database 把 `itemId`、`characterId` 等稳定 ID 还原成运行时对象。现在保存内容包括背包、队伍、Field 状态、教程完成状态和自动剧情完成状态。

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
- Demo end panel.

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

### 13.4 DemoEndPanel

File:

```text
Assets/Scripts/UI/DemoEndController.cs
```

The demo end panel is the current v1 clear screen.

Flow:

```text
FieldAutoDialogueEventController.onDialogueFinished
 -> DemoEndController.ShowDemoEnd()
 -> BasePanel.Show()
 -> FieldPauseState.SetPaused(true)
 -> BackToTitle button
 -> SceneManager.LoadScene("TitleScene")
```

Notes:

- `DemoEndPanel` uses `BasePanel` for CanvasGroup fade and input blocking.
- `DemoEndController` intentionally pauses the Field again because `DialoguePanelController` releases `FieldPauseState` when dialogue ends.
- The panel is a v1 closure point, not a final credits system.

### 13.5 VN Dialogue Panel Preparation

`Boss_FieldScene` currently contains a prepared VN-style dialogue layout:

```text
VNDialoguePanel
├─ BackgroundImage
├─ DimOverlay
├─ LeftPortrait
├─ RightPortrait
└─ DialogueBox
   ├─ SpeakerNameText
   └─ DialogueText
```

Current status:

- The layout exists as scene UI preparation.
- Runtime dialogue still uses `DialoguePanelController`.
- A dedicated `VNDialoguePanelController` is planned for a later cinematic version.

中文：

- `DemoEndPanel` 是 v1 的通关收尾。
- `VNDialoguePanel` 是 v2 演出升级用的 UI 壳子，目前不是主流程必须项。

中文概括：

UI 系统基本都围绕 `BasePanel + CanvasGroup` 做显示和隐藏。Field UI 负责背包、队伍 HUD、E 提示、对话、教程和 Demo 结束面板；Battle UI 负责命令、技能、道具、行动条、奖励、升级和结算。VN 面板目前只是场景里的演出版布局，还没有接运行时控制器。

## 14. Demo v1 Route

The current Demo v1 route is a small graybox RPG flow:

```text
Start
 -> Slime
 -> Chest
 -> Recruit Argo
 -> Group Encounter
 -> Boss Field
 -> Boss
 -> Ending Dialogue
 -> Demo Clear
```

Purpose:

- Slime: regular timed respawn enemy.
- Chest: field interactable, item reward, opened state save.
- Recruit Argo: party member join flow and load rollback test.
- Group Encounter: nearby enemy group detection and visible link lines.
- Boss: permanent clear, boss intro camera, stronger reward.
- Ending Dialogue: auto story event after boss clear.
- Demo Clear: explicit end point for portfolio review.

This route proves that the systems are connected. It is still graybox and not final level art.

Current v1 completion marker:

```text
Boss defeated
 -> boss_spawn_001 cleared
 -> Boss_FieldScene reload / return
 -> Boss ending auto dialogue starts
 -> Dialogue ends
 -> DemoEndPanel appears
```

中文：

```text
Demo v1 的路线已经有明确结束点。
它现在可以作为“系统闭环版”展示，但还不是“美术完成版”。
```

中文概括：

Demo v1 路线的作用是证明系统能串起来，而不是展示最终关卡美术。现在最重要的是确保玩家能沿着路线体验到宝箱、入队、普通战斗、联合遇敌、Boss 战和结尾面板。

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
- Demo scene layout is still graybox.
- DemoEndPanel is a v1 clear screen, not a full credits or ending sequence.
- VNDialoguePanel is prepared in the scene, but runtime VN playback is not implemented yet.
- Chest rewards work, but fully data-driven per-chest reward tables are still planned.
- `EnemyFieldData` remains mainly for older field enemy compatibility.

中文概括：

当前项目已经能作为 v1 系统闭环 demo，但还有不少边界：装备系统未做、Buff 未实现、VN 演出未接入、宝箱奖励还没有完全 table 化、场景仍是灰盒。后续应该优先打磨流程和内容，而不是继续无限扩系统。

## 16. Recommended Next Steps

Short-term portfolio tasks:

1. Run a full Demo v1 regression checklist.
2. Make the graybox route more readable through layout, lighting, signs, and enemy placement.
3. Author battle tutorial content for attack, skill, item, run, and target selection.
4. Polish reward popup text, toast messages, and result flow.
5. Add screenshots or diagrams to README.
6. Decide the first visual direction for Boss Field and VN-style story presentation.

Medium-term system tasks:

1. Convert field enemy behavior from coroutine-driven logic to a state machine.
2. Add a first version of equipment data, equipment slots, and stat modifiers.
3. Expand interactable data for portal / event / quest objects.
4. Add save migration handling using `GameSaveData.version`.
5. Clean up legacy encounter fields after all encounters use `enemyEntries`.
6. Add data-driven chest reward configuration.
7. Replace placeholder dialogue with authored opening and ending text.

中文概括：

短期目标应该是测试和打磨 v1：跑完整回归、整理路线可读性、补教程内容、改善 UI 提示。中期再做状态机、装备、宝箱奖励表、存档迁移和正式剧情文本。

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

中文概括：

这个项目展示的是一个回合制 RPG demo 的系统整合能力：Field 与 Battle 跨场景衔接、ScriptableObject 数据驱动、队伍和背包运行时状态、奖励结算、存档读档、宝箱、入队、教程、对话、联合遇敌、Boss 战和 Demo 通关流程。Demo v1 的价值在于它已经不是单个系统测试，而是一条可以从头走到尾的可玩流程。
