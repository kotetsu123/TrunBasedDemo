# Adventure of Paul Demo

A Unity turn-based RPG prototype focused on data-driven field encounters, runtime party/inventory state, battle rewards, and save/load flow.

## 中文说明

这是一个 Unity 回合制 RPG 系统 demo。当前目标不是完成一款完整游戏，而是把 RPG 中常见的核心系统串成一个可以运行、可以扩展、也适合作品集展示的玩法闭环。

当前 demo 的主要流程是：

- 在 3D Field 场景中探索。
- 通过场景中的敌人或 SpawnPoint 进入遭遇战。
- 切换到回合制 Battle 场景。
- 战斗胜利后结算 EXP、等级、掉落道具和敌人清除状态。
- 回到 Field 场景，并保留队伍、背包、敌人清除和存档数据。

### 核心系统

#### Field / Encounter

Field 中的敌人生成点可以通过 `encounterId` 连接到战斗数据。

```text
FieldData / EnemySpawnPoint
 -> encounterId
 -> EncounterDataBase
 -> EncounterData
 -> BattleSpawner
 -> Battle enemies
```

SpawnPoint 支持两种清除策略：

- `Permanent`：战胜后永久清除，适合 boss 或一次性敌人。
- `Timed`：战胜后经过 `respawnSeconds` 可以重新刷新，适合普通小怪。

Field 场景中也支持 live respawn，也就是玩家不切换场景时，小怪也可以在时间到达后重新生成。

#### FieldData

在当前 3D demo 中，`FieldData` 更像是场景玩法配置表，而不是完整地图生成器。

Unity Scene 负责：

- 地形
- 建筑
- 灯光
- 摄像机
- 碰撞体
- 大型静态场景物件

`FieldData` 负责：

- 敌人生成点
- 遭遇战 id
- 刷新规则
- 可生成的玩法物件入口

这样可以让 3D 场景的美术和空间设计继续由 Unity Scene 控制，同时让敌人、遭遇战和刷新规则保持数据驱动。

#### Battle

Battle 场景负责回合制战斗流程，包括角色生成、回合推进、技能/道具使用、伤害和治疗 floating text、战斗结果和奖励结算。

战斗胜利后会处理：

- 队伍 EXP 增加
- 升级结果
- 掉落道具
- 敌人 SpawnPoint 清除
- PartyRuntimeState 回写

#### Inventory / Party Runtime

背包和队伍状态通过 runtime state 在 Field 与 Battle 之间共享。

Inventory 当前支持：

- slot 数据结构
- item count
- 拖拽换位
- Field 使用道具
- Battle 使用道具
- Save/Load 快照

Party 当前支持：

- characterId
- HP / MP
- EXP / Level
- 战斗后状态回写
- Save/Load 快照
- 复活后保持队伍顺序

#### Reward / Save Load

奖励通过 `EncounterData` 配置，并由 reward service 结算。

当前支持：

- encounter EXP
- item drop
- 掉落概率
- 掉落数量范围
- reward popup
- level-up popup

SaveSystem 会把运行时数据保存成 JSON，包括：

- 背包快照
- 队伍快照
- 当前 Field scene
- 玩家 Field 位置和朝向
- 已清除 SpawnPoint
- Timed respawn 的清除时间

### 当前状态

已实现：

- Field 探索入口
- Field enemy spawn point
- EncounterId 驱动战斗生成
- 回合制战斗流程
- PartyRuntimeState / InventoryRuntimeState
- Field / Battle 道具共享
- EXP 和 item drop
- Reward popup
- Save / Load 核心流程
- Permanent / Timed respawn
- 当前 Field 场景 live respawn
- FieldData 生成 SpawnPoint
- FieldData 生成地图物件入口

计划中：

- 更完整的交互物数据，例如宝箱、NPC、传送点
- 更完整的 Enemy / Encounter 数据结构
- 更完整的 Reward UI
- 更多作品集用截图和架构图

## Project Focus

This project is a portfolio-oriented RPG systems demo. The current scope is not a full game, but a connected gameplay loop:

- Explore a 3D field scene.
- Encounter enemies from field spawn points.
- Enter a turn-based battle.
- Resolve rewards, EXP, level-up results, item drops, and enemy clear state.
- Return to the field with party/inventory/save data preserved.

## Core Systems

### Field Encounter Flow

Field enemies are driven by spawn points. A spawn point can reference an `encounterId`, which links field interaction to battle setup.

```text
FieldData / EnemySpawnPoint
 -> encounterId
 -> EncounterDataBase
 -> EncounterData
 -> BattleSpawner
 -> Battle enemies
```

Spawn points support two clear rules:

- `Permanent`: cleared enemies stay removed, useful for bosses or one-time encounters.
- `Timed`: cleared enemies can respawn after `respawnSeconds`.

The field can also perform live respawn checks, so timed enemies can return while the player remains in the same field scene.

### FieldData

`FieldData` is used as a scene gameplay table rather than a full 3D map generator.

In 3D scenes, heavy environment authoring stays in Unity scenes:

- terrain
- buildings
- lighting
- camera setup
- colliders
- baked or static scene objects

`FieldData` drives gameplay entities:

- enemy spawn points
- encounter ids
- respawn rules
- runtime field object entry points

This keeps the 3D scene art controllable while making gameplay setup data-driven.

### Turn-Based Battle

The battle scene uses generated player and enemy controllers, turn flow, battle camera handling, damage/heal floating text, item usage, result handling, and retry/escape flows.

Battle victory currently connects to:

- party EXP gain
- level-up result collection
- item drops
- enemy spawn clear state
- party runtime state writeback

### Inventory and Items

Inventory data is shared between field and battle through `InventoryRuntimeState`.

Current inventory support includes:

- slot-based runtime item stacks
- item count changes
- drag-and-drop slot movement
- field item usage
- battle item usage
- save/load snapshots

### Party Runtime State

Party data is shared between field and battle through `PartyRuntimeState`.

The party system preserves:

- character id
- HP / MP
- EXP / level
- battle writeback state
- save/load snapshots
- stable party order after battle and revive flows

### Reward Flow

Rewards are configured through `EncounterData` and resolved through a reward service.

Current reward support includes:

- encounter EXP
- item drop entries
- drop chance
- min/max item count
- reward result payload for UI
- reward popup before level-up popup

### Save / Load

The save system serializes runtime gameplay state to JSON.

Current save data includes:

- inventory snapshot
- party snapshot
- current field scene name
- player field position and rotation
- cleared spawn ids
- cleared spawn timestamps for timed respawn

The project uses runtime databases to restore saved item ids and character ids back into gameplay data.

## High-Level Data Flow

```text
Field Scene
 -> FieldCreator
 -> FieldData / EnemySpawnPoint
 -> EnemySpawnManager
 -> EncounterTrigger
 -> FieldBattleContext
 -> Battle Scene
 -> BattleSpawner
 -> BattleManager
 -> EncounterRewardService
 -> PartyRuntimeState / InventoryRuntimeState
 -> SaveSystem
 -> Field Scene
```

## Current Demo Status

Implemented:

- Field exploration entry
- Field enemy spawn points
- Encounter id based battle generation
- Turn-based battle flow
- Party runtime state sharing
- Inventory runtime state sharing
- Field item usage
- Battle item usage
- Reward EXP and item drops
- Result reward popup
- Save/load core flow
- Permanent and timed enemy respawn
- Live field respawn
- FieldData-driven spawn point generation
- FieldData object generation entry point

In progress / planned:

- FieldData player start or scene entry point rules
- More complete interactable data entries, such as chests, NPCs, and portals
- More polished reward UI
- Expanded enemy and encounter data structure
- README diagrams and gameplay screenshots

## Notes

This demo intentionally separates heavy 3D scene authoring from gameplay data.

The Unity scene remains responsible for visual composition and static environment setup, while ScriptableObject data handles gameplay configuration and runtime systems.
