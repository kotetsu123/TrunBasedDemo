# Boss Field Scene Setup

## Purpose

This document describes the first setup plan for the Boss Field scene.

The Boss Field is not the main maze. It is a smaller scene used to connect the end of the maze, the boss encounter, and the final demo ending flow.

中文说明：

```text
Boss Field 不是现在的迷宫主场景。
它更像是火山核心 / Boss 门前区域，用来承接迷宫出口、Boss 战和 Ending Trigger。
```

## Target Flow

```text
Title
 -> Field_Maze
 -> Boss_Field
 -> Boss Battle
 -> Boss_Field
 -> Ending Trigger
 -> Ending Dialogue / Demo End
```

The maze scene should end with a scene transition, not the ending trigger.

## Scene Responsibility

### Field_Maze

The current maze-like Field scene should focus on exploration and system demonstration:

- Movement tutorial.
- Chest interaction.
- Regular enemy encounter.
- Recruit point.
- Group encounter.
- Save / Load regression.
- Exit trigger to Boss Field.

### Boss_Field

The Boss Field scene should focus on the demo climax:

- Player spawn after leaving the maze.
- Boss encounter point.
- Optional boss gate dialogue.
- Return position after boss battle.
- Ending trigger after the boss is cleared.

## Recommended Hierarchy

```text
BossFieldScene
├─ Environment
│  ├─ Ground
│  ├─ ArenaBlockout
│  ├─ Walls
│  └─ Props
├─ SpawnPoints
│  ├─ PlayerStartPoint
│  ├─ BossSpawnPoint
│  ├─ DialogueTriggers
│  └─ EndingTriggers
├─ RuntimeGenerated
│  ├─ GeneratedSpawnPoints
│  ├─ GeneratedInteractables
│  └─ GeneratedEnvironment
├─ Managers
│  ├─ FieldCreator
│  ├─ EnemySpawnManager
│  ├─ SceneTransitionController
│  ├─ FieldSaveContext
│  └─ EventSystem
├─ Player
└─ FieldCanvas
   ├─ FieldPartyHud
   ├─ ItemInventoryPanel
   ├─ FieldEscMenuPanel
   ├─ FieldToastPanel
   ├─ DialoguePanel
   ├─ TutorialPanel
   ├─ EndingPanel
   └─ FadeCanvas
```

Notes:

- `Environment` is for hand-authored scene art and collision.
- `SpawnPoints` is for visible editor markers and important gameplay positions.
- `RuntimeGenerated` is where `FieldCreator` places generated points and interactables.
- `Managers` should stay mostly logic-only.

## FieldCreator Setup

Attach or reuse `FieldCreator` in the Boss Field scene.

Inspector references:

```text
fieldData
 -> FieldData_Boss

generatedSpawnPointRoot
 -> RuntimeGenerated/GeneratedSpawnPoints

generatedInteractableRoot
 -> RuntimeGenerated/GeneratedInteractables

generatedEnvironmentRoot
 -> RuntimeGenerated/GeneratedEnvironment

player
 -> Player transform

playerStartPoint
 -> SpawnPoints/PlayerStartPoint

enemySpawnManager
 -> Managers/EnemySpawnManager

characterDataBase
 -> Character Data Base

initialPartyData
 -> InitialPartyData_default

partyHudController
 -> FieldCanvas/FieldPartyHud

initialItems
 -> usually empty if the maze already gave items
```

中文重点：

```text
Boss Field 也可以继续用 FieldCreator。
只是这里的 FieldData_Boss 只负责 Boss 区需要的少量 gameplay 点，不负责生成完整 3D 地图。
```

## FieldData_Boss Setup

Create a new `FieldData` asset:

```text
Assets/Scripts/Data/FieldData/FieldData_Boss.asset
```

Suggested fields:

```text
fieldId: Boss_Field
```

### Spawn Points

Add one boss spawn point:

```text
spawnId: boss_spawn_001
encounterId: Encounter_Boss001
fieldPrefab: Boss field enemy prefab
position: boss marker position
rotationEuler: face toward player route
wanderRadius: 0 or small value
enemyId: empty for new encounter-driven flow
respawnType: Permanent
respawnSeconds: 0
```

Explanation:

- `spawnId` is the save/runtime identity of the field boss point.
- `encounterId` tells Battle which encounter table to load.
- `respawnType = Permanent` means the boss stays cleared after victory.
- `enemyId` should usually be empty for new content because Battle enemies come from `EncounterData.enemyEntries`.

### Field Objects

First version can be empty.

If the ending trigger already has a prefab, add it here:

```text
objectId: ending_trigger_001
prefab: EndingTrigger prefab
position: after-boss route position
rotationEuler: 0, 0, 0
scale: 1, 1, 1
```

If there is no ending prefab yet, place it manually in the scene first.

### Recruit Points

Usually empty for Boss Field first version.

If a story scene later needs a party member to join before the boss, add it here.

## EncounterData_Boss Setup

Create or confirm:

```text
Encounter_Boss001
```

Recommended first version:

```text
encounterId: Encounter_Boss001
enemyEntries:
  - enemyId: boss_001
    count: 1
rewardExp: boss reward amount
itemDrops: optional
introCameraType: Boss
```

Enemy data source:

```text
EncounterData.enemyEntries.enemyId
 -> EnemyCharacterDataBase
 -> Character.characterId
```

So `boss_001` must exist as a character entry in `EnemyCharacterDataBase`.

## Scene Transition Setup

The maze exit should transition to Boss Field.

Recommended setup:

```text
Field_Maze ExitTrigger
 -> target scene: Boss_Field
 -> fade before loading
```

Boss Field should have:

```text
PlayerStartPoint
 -> placed near the entrance from the maze
```

After the boss battle, `FieldBattleContext` should return the player to the saved pre-battle position in Boss Field.

## Ending Trigger Placement

Ending Trigger should be placed after the boss path, not at the maze exit.

Recommended first layout:

```text
Boss Field Entrance
 -> Boss Spawn Point
 -> short path forward
 -> Ending Trigger
```

The player should only reach or use the ending trigger after the boss is cleared.

First version options:

- Put the trigger behind the boss.
- Use level geometry so the player naturally reaches it after victory.
- Later, add a proper lock condition such as `boss_spawn_001` cleared.

## First Test Plan

- [ ] Enter Boss Field from the maze exit.
- [ ] Player appears at Boss Field `PlayerStartPoint`.
- [ ] Boss field enemy is generated from `FieldData_Boss`.
- [ ] Boss field enemy uses `spawnId = boss_spawn_001`.
- [ ] Boss battle loads `Encounter_Boss001`.
- [ ] Boss battle uses `introCameraType = Boss`.
- [ ] Winning the boss battle returns to Boss Field.
- [ ] Boss does not respawn after victory if `respawnType = Permanent`.
- [ ] Ending Trigger can be reached after boss victory.
- [ ] Ending dialogue / Demo End panel appears.

中文测试清单：

- [ ] 可以从迷宫出口进入 Boss Field。
- [ ] 玩家出现在 Boss Field 的 `PlayerStartPoint`。
- [ ] Boss 场景敌人由 `FieldData_Boss` 生成。
- [ ] Boss 场景敌人的 `spawnId` 是 `boss_spawn_001`。
- [ ] 进入战斗后读取的是 `Encounter_Boss001`。
- [ ] Boss 战使用 Boss 类型镜头演出。
- [ ] Boss 战胜利后返回 Boss Field。
- [ ] 如果 `respawnType = Permanent`，Boss 胜利后不会重新刷新。
- [ ] Boss 胜利后可以到达 Ending Trigger。
- [ ] Ending dialogue / Demo End 面板可以显示。

## Current Boundary

## 中文配置版

这一版 Boss Field 的目标不是做完整美术场景，而是先把 demo 的高潮流程接起来。

推荐流程：

```text
火山迷宫
 -> 迷宫出口
 -> Boss Field
 -> Boss 战
 -> 回到 Boss Field
 -> Ending Trigger
 -> 结尾对话 / Demo End
```

### 1. Boss Field 是什么

`Boss_Field` 可以理解成一个单独的小场景：

- 它不是现在的大迷宫。
- 它只负责 Boss 前区域、Boss 触发点和结尾触发点。
- 它可以继续使用 `FieldCreator`，但不需要让 `FieldCreator` 生成完整地图。
- 地面、墙、火山装饰、碰撞体这些大型环境，还是建议手动摆在 Unity Scene 里。
- Boss、EndingTrigger、少量交互点，才适合用 `FieldData_Boss` 管理。

### 2. Hierarchy 建议

Boss 场景可以先按这个结构摆：

```text
BossFieldScene
├─ Environment
│  ├─ Ground
│  ├─ ArenaBlockout
│  ├─ Walls
│  └─ Props
├─ SpawnPoints
│  ├─ PlayerStartPoint
│  ├─ BossSpawnPoint
│  ├─ DialogueTriggers
│  └─ EndingTriggers
├─ RuntimeGenerated
│  ├─ GeneratedSpawnPoints
│  ├─ GeneratedInteractables
│  └─ GeneratedEnvironment
├─ Managers
│  ├─ FieldCreator
│  ├─ EnemySpawnManager
│  ├─ SceneTransitionController
│  ├─ FieldSaveContext
│  └─ EventSystem
├─ Player
└─ FieldCanvas
```

简单理解：

- `Environment`：手动摆的地图、美术、墙、地面。
- `SpawnPoints`：你在编辑器里用来对位置的点。
- `RuntimeGenerated`：运行时生成出来的敌人、宝箱、入队点等。
- `Managers`：只放逻辑控制脚本，不放实际场景物件。

### 3. FieldCreator 要拖什么

在 Boss Field 场景里，`FieldCreator` 可以这样配置：

```text
fieldData
 -> FieldData_Boss

generatedSpawnPointRoot
 -> RuntimeGenerated/GeneratedSpawnPoints

generatedInteractableRoot
 -> RuntimeGenerated/GeneratedInteractables

generatedEnvironmentRoot
 -> RuntimeGenerated/GeneratedEnvironment

player
 -> Player

playerStartPoint
 -> SpawnPoints/PlayerStartPoint

enemySpawnManager
 -> Managers/EnemySpawnManager

characterDataBase
 -> Character Data Base

initialPartyData
 -> InitialPartyData_default

partyHudController
 -> FieldCanvas/FieldPartyHud
```

`initialItems` 第一版可以先留空。因为如果玩家是从迷宫过来的，道具应该已经存在于 `InventoryRuntimeState` 里。

### 4. FieldData_Boss 要写什么

建议新建：

```text
Assets/Scripts/Data/FieldData/FieldData_Boss.asset
```

然后设置：

```text
fieldId: Boss_Field
```

Boss 敌人的 `Spawn Points` 可以这样写：

```text
spawnId: boss_spawn_001
encounterId: Encounter_Boss001
fieldPrefab: Boss 的场景敌人 prefab
position: Boss 出现的位置
rotationEuler: 朝向玩家路线
wanderRadius: 0 或者很小
enemyId: 留空
respawnType: Permanent
respawnSeconds: 0
```

这里最重要的是：

- `spawnId`：Field 里这个 Boss 生成点的存档 ID。
- `encounterId`：进入 Battle 时读取哪个 EncounterData。
- `respawnType = Permanent`：Boss 打赢后不会再刷新。
- `enemyId`：新流程可以先留空，因为战斗里的敌人来自 `EncounterData.enemyEntries`。

### 5. Boss EncounterData 要怎么配

建议准备一个：

```text
Encounter_Boss001
```

内容大概是：

```text
encounterId: Encounter_Boss001
enemyEntries:
  - enemyId: boss_001
    count: 1
rewardExp: Boss 奖励经验
itemDrops: 可选
introCameraType: Boss
```

注意数据流：

```text
FieldData_Boss.spawnPoints.encounterId
 -> EncounterDataBase 找 Encounter_Boss001
 -> EncounterData.enemyEntries 读取 boss_001
 -> EnemyCharacterDataBase 找 characterId = boss_001 的敌人模板
 -> BattleSpawner 生成 Boss
```

所以 `EnemyCharacterDataBase` 里也需要有一个 `characterId = boss_001` 的敌人数据。

### 6. Ending Trigger 放哪里

Ending Trigger 不建议放在迷宫出口。

更推荐：

```text
Boss Field 入口
 -> BossSpawnPoint
 -> Boss 战
 -> 返回 Boss Field
 -> 往前走一点
 -> EndingTrigger
```

第一版可以先用地形路线限制玩家，让玩家自然在打完 Boss 后走到 EndingTrigger。

之后如果要更严谨，可以给 EndingTrigger 加条件：

```text
只有 boss_spawn_001 已经 cleared，才允许触发 ending。
```

### 7. 第一版最低目标

先做到这些就可以：

- 可以从迷宫进入 Boss Field。
- 玩家出现在 Boss Field 的出生点。
- Boss 由 `FieldData_Boss` 生成。
- 碰到 Boss 后进入 `Encounter_Boss001`。
- Boss 战使用 Boss 镜头。
- Boss 胜利后返回 Boss Field。
- Boss 胜利后不再刷新。
- 可以触发 Ending。

不用一开始就做：

- 完整火山美术。
- Boss 多阶段机制。
- 复杂剧情演出。
- 外部 Excel 表格导入。
- 完整 VN 式立绘系统。

This setup does not require a full second polished level yet.

First version can be:

```text
small graybox arena
 -> one boss spawn point
 -> one ending trigger
```

The important part is to prove the demo structure:

```text
Maze exploration leads to a separate boss scene, and the ending happens after the boss.
```
