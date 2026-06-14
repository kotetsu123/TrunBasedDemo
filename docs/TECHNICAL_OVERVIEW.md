# Adventure of Paul Demo Technical Overview

## 1. 文档目的

本文档用于说明 `Adventure of Paul Demo` 当前项目的核心技术结构。

项目定位是一个 Unity 回合制 RPG 系统 demo，重点不是完整游戏内容，而是展示一套可以运行、可以扩展的 RPG gameplay loop：

```text
Field 探索
 -> 敌人遭遇
 -> Battle 回合制战斗
 -> 奖励结算
 -> Party / Inventory 状态回写
 -> Save / Load
 -> 返回 Field
```

## 2. 总体架构

当前项目可以分成以下几个系统：

| 系统 | 主要职责 | 代表脚本 |
| --- | --- | --- |
| Field System | Field 场景初始化、玩家位置、敌人生成点、遭遇入口 | `FieldCreator`, `FieldData`, `EnemySpawnManager`, `EncounterTrigger` |
| Battle System | 回合制战斗、行动条、目标选择、战斗结束流程 | `BattleManager`, `BattleSpawner`, `BattleFormation`, `BattleTargetSelector` |
| Runtime State | 跨场景保存运行时队伍和背包状态 | `PartyRuntimeState`, `InventoryRuntimeState` |
| Encounter / Reward | 遭遇战数据、敌人队伍、EXP 和掉落 | `EncounterData`, `EncounterDataBase`, `EncounterRewardService` |
| Save / Load | JSON 存档、运行时快照恢复 | `SaveSystem`, `GameSaveData`, `FieldSaveData`, `InventorySaveData`, `PartySaveData` |
| UI System | 战斗 UI、Field 背包、奖励弹窗、结果面板 | `BasePanel`, `ResultCharacterPanelController`, `RewardPopController`, `FieldInventoryPanelController` |

核心数据流：

```text
FieldData / EnemySpawnPoint
 -> EnemySpawnManager
 -> EnemyFieldController
 -> EncounterTrigger
 -> FieldBattleContext
 -> BattleSpawner
 -> BattleManager
 -> EncounterRewardService
 -> PartyRuntimeState / InventoryRuntimeState
 -> SaveSystem
```

## 3. Field System

### 3.1 FieldCreator

文件：

```text
Assets/Scripts/Filed/FieldCreator.cs
```

`FieldCreator` 是 Field 场景初始化入口。

当前职责：

- 清理 Field 暂停状态。
- 初始化 `PartyRuntimeState`。
- 初始化 `InventoryRuntimeState`。
- 根据 `FieldData` 动态生成 SpawnPoint。
- 根据 `FieldData` 动态生成 FieldObject。
- 设置玩家初始位置。
- 调用 `EnemySpawnManager.SpawnAll()` 生成 Field 敌人。
- 刷新 Field Party HUD。
- 处理战斗返回后的 encounter cooldown。

初始化顺序：

```text
FieldPauseState.Clear()
 -> PartyRuntimeState.InitializeIfEmpty()
 -> InventoryRuntimeState.InitializeIfEmpty()
 -> CreateSpawnPointsFromFieldData()
 -> CreateObjectsFromFieldData()
 -> SetupPlayer()
 -> EnemySpawnManager.SpawnAll()
 -> Party HUD Refresh
 -> Battle return cooldown
```

玩家位置优先级：

```text
1. Battle return position
2. Save / Load position
3. Scene playerStartPoint
```

当前没有把 player start 放进 `FieldData`，因为本项目是 3D 箱庭式场景，出生点用 Scene 中的 Transform 手动摆放更直观。

### 3.2 FieldData

文件：

```text
Assets/Scripts/Filed/FieldData.cs
```

`FieldData` 是当前 Field 场景的 gameplay table，不是完整 3D 地图生成器。

在 3D 场景中：

```text
Unity Scene:
- 地形
- 建筑
- 灯光
- 摄像机
- 静态碰撞
- 大型静态场景物件

FieldData:
- 敌人生成点
- encounterId
- respawn 规则
- gameplay object 生成入口
```

当前包含：

```text
FieldData
- fieldId
- spawnPoints: List<FieldSpawnPointEntry>
- fieldObjects: List<FieldObjectEntry>
```

`FieldSpawnPointEntry` 字段：

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

`FieldObjectEntry` 字段：

```text
objectId
prefab
position
rotationEuler
scale
```

设计意图：

- 3D demo 中，不强制用 FieldData runtime 生成整个世界。
- FieldData 主要驱动 gameplay entity。
- 以后迁移到 2D / tilemap / roguelike 时，可以扩展为更完整的 map table。

### 3.3 EnemySpawnPoint

文件：

```text
Assets/Scripts/Filed/EnemySpawn/EnemySpawnPoint.cs
```

`EnemySpawnPoint` 是 Field 中敌人生成点配置。

关键字段：

```text
spawnId
encounterId
fieldPrefab
wanderRadius
enemyId
respawnType
respawnSeconds
```

`respawnType`：

```text
Permanent: 战胜后永久清除，适合 boss / 一次性敌人
Timed: 战胜后经过 respawnSeconds 后可以刷新，适合普通小怪
```

兼容策略：

- 新流程优先使用 `encounterId + fieldPrefab`。
- 老流程可以通过 `enemyId` 从 `EnemyDataBase` 中补充 `fieldPrefab / encounterId / wanderRadius`。

### 3.4 EnemySpawnManager

文件：

```text
Assets/Scripts/Filed/EnemySpawn/EnemySpawnManager.cs
```

`EnemySpawnManager` 负责把 `EnemySpawnPoint` 转成 Field 上可碰撞、可游荡的敌人实例。

核心职责：

- `SpawnAll()`：遍历当前 SpawnPoint 并生成敌人。
- `SetSpawnPoints()`：接收 FieldData 动态生成的 SpawnPoint 列表。
- `TrySpawnPoint()`：单个 SpawnPoint 生成流程。
- Live Respawn：按 `liveRespawnCheckInterval` 定期检查 Timed SpawnPoint。
- `activeEnemiesBySpawnId`：记录当前场景内每个 SpawnPoint 已经生成的敌人，避免重复生成。

生成判断：

```text
HasActiveEnemy(spawnId)
 -> true: 当前场上已经有该 SpawnPoint 的敌人，不生成
 -> false: 继续判断清除/刷新规则

FieldBattleContext.ShouldSkipSpawn(...)
 -> true: 当前仍应跳过生成
 -> false: 允许生成
```

### 3.5 FieldBattleContext

文件：

```text
Assets/Scripts/Filed/FieldBattleContext.cs
```

`FieldBattleContext` 是 Field 与 Battle 之间的静态上下文。

保存内容：

- 战斗前 Field scene 名称。
- 战斗前玩家位置和朝向。
- 触发战斗的 `spawnId`。
- 当前 `encounterId`。
- 战斗返回后的 encounter cooldown。
- 已清除 SpawnPoint 集合。
- Timed respawn 的清除时间。
- 从存档恢复的玩家位置。

核心方法：

```text
SaveFieldReturnData()
MarkTriggerdEnemyCleared()
ShouldSkipSpawn()
ToSaveData()
LoadFromSaveData()
ClearReturnData()
ClearAll()
```

Respawn 判断逻辑：

```text
没有 spawnId
 -> 不跳过，允许生成

spawnId 没在 clearedSpawnIds
 -> 没被打败过，允许生成

已清除 + 不可刷新
 -> 跳过生成

已清除 + 可刷新 + 时间未到
 -> 跳过生成

已清除 + 可刷新 + 时间已到
 -> 移除清除记录，允许生成
```

### 3.6 EncounterTrigger

文件：

```text
Assets/Scripts/Filed/Encounter/EncounterTrigger.cs
```

`EncounterTrigger` 负责 Field 敌人与玩家碰撞后进入 Battle。

流程：

```text
Player enters trigger
 -> 检查 FieldPauseState
 -> 检查 encounter cooldown
 -> 从 EnemyFieldController 读取 spawnId / encounterId
 -> FieldBattleContext.SaveFieldReturnData()
 -> 禁用玩家移动
 -> SceneTransitionController.StartBattleTransition()
```

## 4. Battle System

### 4.1 BattleSpawner

文件：

```text
Assets/Scripts/Battle/BattleSpawner.cs
```

`BattleSpawner` 负责生成战斗单位。

玩家生成：

- 从 `PartyRuntimeState.PartyMembers` 读取当前队伍。
- 复制角色数据后生成 Battle Controller。
- 按顺序放入 `BattleFormation`。

敌人生成：

```text
FieldBattleContext.CurrentEncounterId
 -> EncounterDataBase.FindeById()
 -> EncounterData.EnemyChatacters
 -> enemyPrefab
 -> SpawnRequest
 -> BattleFormation slot
```

Fallback：

- 如果没有 `CurrentEncounterId`。
- 如果 `EncounterDataBase` 为空。
- 如果找不到 EncounterData。
- 如果 EncounterData 配置无效。
- 如果 `enemyPrefab` 为空。

则使用 `initialEnemies` 测试配置。

### 4.2 BattleManager

文件：

```text
Assets/Scripts/Battle/BattleManager.cs
```

`BattleManager` 是战斗主流程控制器。

当前职责：

- 注册角色 Controller。
- 管理行动条和行动值。
- 发布 Timeline 顺序。
- 管理当前行动者、目标、预览目标。
- 接收 Battle UI 指令。
- 执行 Attack / Skill / Item。
- 处理战斗结束。
- 广播 `OnBattleEnded`。

战斗结束流程：

```text
CheckBattleEnd()
 -> HandleBattleEnd(result)
 -> Win:
      FieldBattleContext.MarkTriggerdEnemyCleared()
      EncounterRewardService.GrantRewards()
      PartyRuntimeState.UpdateFromBattleController()
 -> BuildPartySnapShots()
 -> BattleResultPayload
 -> OnBattleEnded
```

Lose / Retry：

- Lose 不回写 PartyRuntimeState。
- Retry 使用进入战斗前的 runtime state。

### 4.3 BattleFormation

文件：

```text
Assets/Scripts/Battle/BattleFormation.cs
```

`BattleFormation` 管理战斗站位 slot。

职责：

- 查找空 slot。
- 占用 slot。
- 释放 slot。
- 提供站位 anchor。
- 在 slot 变化时通知 BattleSpawner 补位。

### 4.4 Battle UI / Result Flow

代表文件：

```text
Assets/Scripts/Manager/ResultCharacterPanelController.cs
Assets/Scripts/UI/RewardPopController.cs
Assets/Scripts/UI/LevelUpPopController.cs
```

结果流程：

```text
BattleEndPanel
 -> SettlePanel
 -> RewardPop
 -> LevelUpPop
 -> Return Field / Retry / Title / Load
```

RewardPop 使用 `EncounterRewardResult` 显示本场获得的 EXP 和掉落道具。

## 5. Encounter / Reward System

### 5.1 EncounterData

文件：

```text
Assets/Scripts/Filed/Encounter/EncounterData.cs
```

`EncounterData` 是遭遇战配置。

字段：

```text
encounterId
enemyChatacters
rewardExp
itemDrops
```

说明：

- 当前版本中 `EncounterData` 直接引用 `Character` 作为敌人模板。
- 后续可以进一步拆成 `enemyId -> EnemyDatabase -> Character template`。

### 5.2 EncounterDataBase

文件：

```text
Assets/Scripts/Filed/Encounter/EncounterDataBase.cs
```

`EncounterDataBase` 通过 `encounterId` 查找对应 EncounterData。

已有防呆：

- 空 id 警告。
- 找不到 id 警告。
- 重复 id 警告。

### 5.3 EncounterRewardService

文件：

```text
Assets/Scripts/Filed/Encounter/EncounterData.cs
```

`EncounterRewardService` 当前和 `EncounterData` 放在同一个文件中。

职责：

- 根据 EncounterData 决定 EXP。
- 给玩家队伍 Battle Controller 加 EXP。
- 收集 LevelUpResult。
- Roll item drop。
- 把掉落道具写入 `InventoryRuntimeState`。
- 返回 `EncounterRewardResult` 给 BattleManager / UI。

掉落逻辑：

```text
foreach EncounterItemDrop
 -> Random.value <= dropChance
 -> Random.Range(minCount, maxCount + 1)
 -> InventoryRuntimeState.AddItem()
```

## 6. Runtime State

### 6.1 InventoryRuntimeState

文件：

```text
Assets/Scripts/Filed/Inventory/InventoryRuntimeState.cs
```

当前背包以有序 slot 列表保存，而不是 Dictionary。

原因：

- UI slot 顺序需要稳定。
- 拖拽换位需要保存 slot index。
- Save / Load 需要恢复布局。

核心能力：

- 初始化固定容量。
- 添加物品。
- 消耗物品。
- 查询数量。
- slot swap。
- 保存为 InventorySaveData。
- 从 InventorySaveData 恢复。

当前规则：

- 相同 `ItemData` 会堆叠到已有 slot。
- 空 slot 会保留 index。
- 容量满时当前版本会扩容并 warning。

### 6.2 PartyRuntimeState

文件：

```text
Assets/Scripts/Data/RunTime/PartyRuntimeState.cs
```

`PartyRuntimeState` 保存跨场景的队伍状态。

核心能力：

- 初始化队伍。
- 从 Battle Controller 回写队伍。
- Field 中使用道具治疗队伍成员。
- 保存为 PartySaveData。
- 从 PartySaveData 恢复。

回写策略：

- 优先用 `characterId` 匹配原队伍成员。
- fallback 到 `Name` 匹配旧数据。
- 保持原始队伍顺序。
- 新加入角色追加到队伍后面。

## 7. Save / Load System

### 7.1 SaveSystem

文件：

```text
Assets/Scripts/Manager/SaveSystem.cs
```

存档文件：

```text
Application.persistentDataPath/save.json
```

保存流程：

```text
SaveSystem.Save()
 -> BuildSaveData()
 -> InventoryRuntimeState.ToSaveData()
 -> PartyRuntimeState.ToSaveData()
 -> FieldBattleContext.ToSaveData()
 -> FieldSaveContext.TryFillFieldSaveData()
 -> JsonUtility.ToJson()
 -> File.WriteAllText()
```

读取流程：

```text
SaveSystem.Load(itemDatabase, characterDatabase)
 -> File.ReadAllText()
 -> JsonUtility.FromJson<GameSaveData>()
 -> InventoryRuntimeState.LoadFromSaveData()
 -> PartyRuntimeState.LoadFromSaveData()
 -> FieldBattleContext.LoadFromSaveData()
 -> FieldSaveContext.TryApplySavedPlayerTransform()
```

### 7.2 SaveData DTO

文件：

```text
Assets/Scripts/Data/SaveData/GameSaveData.cs
Assets/Scripts/Data/SaveData/InventorySaveData.cs
Assets/Scripts/Data/SaveData/PartyMemberSaveData.cs
Assets/Scripts/Data/SaveData/FieldSaveData.cs
```

`GameSaveData`：

```text
version
inventory
party
field
```

`InventorySaveData`：

```text
slots[]
 -> itemId
 -> count
```

`PartySaveData`：

```text
members[]
 -> characterId
 -> hp / maxHp
 -> mp / maxMp
 -> level / exp
 -> isDead
```

`FieldSaveData`：

```text
clearedSpawnIds
clearedSpawnRecords
sceneName
playerPos
playerRotEuler
hasPlayerTransform
```

### 7.3 Database 恢复

存档不直接保存 ScriptableObject 引用，而保存稳定 id。

恢复时：

```text
itemId -> ItemDataBase -> ItemData
characterId -> CharacterDataBase -> Character
```

这让 JSON 存档更稳定，也避免直接序列化 Unity Object 引用。

## 8. UI System

### 8.1 BasePanel

文件：

```text
Assets/Scripts/UI/BasePanel.cs
```

`BasePanel` 统一面板显示/隐藏逻辑，通常通过 CanvasGroup 控制：

- alpha
- interactable
- blocksRaycasts

### 8.2 Field Inventory UI

代表文件：

```text
Assets/Scripts/Filed/Inventory/FieldInventoryPanelController.cs
Assets/Scripts/Filed/Inventory/FieldInventoryItemView.cs
Assets/Scripts/Filed/Inventory/DraggableItem.cs
Assets/Scripts/Filed/Inventory/InventorySlot.cs
Assets/Scripts/Filed/Inventory/FieldInventoryPartyTargetPanelController.cs
```

当前能力：

- 动态显示 InventoryRuntimeState slot。
- 空 slot 隐藏 icon/count。
- 点击 item 显示 description。
- Use 后弹出 party target panel。
- 拖拽 item root 交换 slot。
- Esc / B 控制背包关闭。

### 8.3 Battle UI

代表文件：

```text
BattleCommandPanel
SkillPanelController
ItemPanelController
TimeLineUI
RewardPopController
LevelUpPopController
ResultCharacterPanelController
```

Battle UI 通过事件与 `BattleManager` 交互。

## 9. Data Asset Map

当前项目主要 ScriptableObject 数据：

```text
Character
CharacterDataBase
PartyInitialData
ItemData
ItemDataBase
SkillData
EnemyFieldData
EnemyDataBase
EncounterData
EncounterDataBase
FieldData
```

推荐理解方式：

```text
CharacterDataBase:
    角色模板恢复，用于 Party Save/Load。

ItemDataBase:
    道具模板恢复，用于 Inventory Save/Load。

EnemyDataBase / EnemyFieldData:
    Field 旧流程兼容，用 enemyId 补充 fieldPrefab / encounterId。

EncounterDataBase:
    Battle 生成入口，用 encounterId 找 EncounterData。

FieldData:
    Field scene gameplay table，用于生成 SpawnPoint 和 gameplay object。
```

## 10. 主要流程图

### 10.1 Field 到 Battle

```text
FieldCreator
 -> EnemySpawnManager.SpawnAll()
 -> EnemyFieldController.Init(spawnId, encounterId)
 -> EncounterTrigger.OnTriggerEnter(Player)
 -> FieldBattleContext.SaveFieldReturnData()
 -> Load BattleScene
 -> BattleSpawner.SpawnInitial()
 -> BattleSpawner.TrySpawnEnemiesFromEncounter()
```

### 10.2 Battle 胜利到 Field

```text
BattleManager.CheckBattleEnd()
 -> HandleBattleEnd(Win)
 -> FieldBattleContext.MarkTriggerdEnemyCleared()
 -> EncounterRewardService.GrantRewards()
 -> PartyRuntimeState.UpdateFromBattleController()
 -> Result UI
 -> Return Field
 -> FieldCreator.SetupPlayer()
 -> EnemySpawnManager.SpawnAll()
 -> FieldBattleContext.ShouldSkipSpawn()
```

### 10.3 Save / Load

```text
Save:
RuntimeState / FieldBattleContext
 -> GameSaveData
 -> JSON
 -> persistentDataPath/save.json

Load:
save.json
 -> GameSaveData
 -> ItemDataBase / CharacterDataBase restore
 -> InventoryRuntimeState / PartyRuntimeState / FieldBattleContext
 -> Field player transform apply
```

## 11. 当前兼容策略

### FieldData 兼容

```text
FieldCreator.fieldData == null:
    使用旧场景手摆 SpawnPoint。

FieldCreator.fieldData != null:
    使用 FieldData 动态生成 SpawnPoint，并覆盖 EnemySpawnManager 的 spawnPoints。
```

### Encounter 兼容

```text
BattleSpawner 找到 EncounterData:
    使用 encounter enemy list 生成敌人。

BattleSpawner 找不到 EncounterData:
    fallback 到 initialEnemies。
```

### Save 兼容

```text
旧存档只有 clearedSpawnIds:
    Load 时为 timed respawn 补当前 UTC 时间。

新存档有 clearedSpawnRecords:
    使用 saved clearedAtUtc 判断 respawn 时间。
```

## 12. 当前边界和后续扩展

### 当前边界

- `EncounterData` 目前直接引用 `Character`，还没有完全拆成 `enemyId -> EnemyDatabase -> enemy template`。
- `FieldData` 目前是 gameplay table，不是完整 3D map generator。
- Field object entry 只是入口，宝箱/NPC/传送点等交互逻辑还未细化。
- Inventory full flow 目前是自动扩容并 warning。
- Reward UI 已有第一版，但还可以继续做更完整的奖励面板。

### 推荐后续扩展

1. `EnemyData` / `EncounterData` 第二版  
   把 EncounterData 中的敌人从直接 Character 引用，改成 enemyId / quantity / position。

2. Interactable Data  
   在 FieldData 中增加 chest / NPC / portal 等 gameplay object 类型。

3. Reward Panel 第二版  
   统一 EXP、Item、Gold、LevelUp 的展示顺序。

4. Save Migration  
   使用 `GameSaveData.version` 为未来存档格式变化做兼容。

5. Portfolio Diagrams  
   在 README 或 docs 中加入截图和架构图，展示完整 gameplay loop。

## 13. 作品集说明角度

这个项目适合强调以下技术点：

- Unity ScriptableObject data-driven workflow。
- Field 与 Battle 跨场景状态同步。
- 回合制战斗 timeline 和 target selection。
- Runtime inventory slot state。
- JSON save/load DTO 设计。
- Encounter reward service 解耦。
- Permanent / Timed enemy respawn。
- FieldData 作为 3D scene gameplay table 的工程判断。

一句话概括：

```text
This project keeps heavy 3D scene authoring inside Unity scenes, while making RPG gameplay entities, encounters, rewards, runtime state, and save data configurable and reusable through data-driven systems.
```
