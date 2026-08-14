# Volcano Prologue Demo Plan

## 目的

这份文档用来记录火山开篇 demo 的第一版灰盒路线。

目标不是马上做出最终美术，而是先把项目现有系统串成一条可以从开头玩到结尾的流程。场景可以先很粗糙，但每个 gameplay 点要清楚。

## 核心概念

玩家一开始进入火山区域。这里不是普通新手村，而是接近故事高潮的倒叙开篇。

推荐结构：

```text
火山入口
 -> 基础移动 / Tutorial
 -> 宝箱
 -> 普通小怪
 -> Argo 入队
 -> 联合遇敌区域
 -> Boss 门前剧情
 -> Boss 战
 -> Ending Trigger
 -> 倒叙转场
```

这个流程同时展示：

- Field exploration
- Tutorial
- Chest reward
- Inventory / Item
- Battle
- Recruit
- Dialogue
- Group encounter
- Boss encounter
- Reward / Drop
- Save / Load
- Ending trigger

## 第一版灰盒路线

先用 Cube、Plane、Terrain、简单墙体和路障搭路线，不追求美术完成度。

```text
[Start]
   |
   v
[Tutorial Gate]
   |
   v
[Chest Point]
   |
   v
[Slime Encounter]
   |
   v
[Recruit Argo]
   |
   v
[Group Encounter]
   |
   v
[Boss Gate Dialogue]
   |
   v
[Boss Encounter]
   |
   v
[Ending Trigger]
```

## Unity Hierarchy 建议

```text
FieldScene
- Environment
  - Ground
  - RouteWalls
  - VolcanoProps
  - LavaVisuals
- SpawnPoints
  - PlayerStartPoint
  - EnemySpawnPoints
  - ChestSpawnPoints
  - RecruitSpawnPoints
  - DialogueTriggers
  - EndingTriggers
- RuntimeGeneratedObjects
  - GeneratedEnemies
  - GeneratedChests
  - GeneratedRecruits
  - GeneratedFieldObjects
- Managers
  - FieldCreator
  - EnemySpawnManager
  - SceneTransitionController
```

原则：

- `Environment` 放手摆的静态场景物件。
- `SpawnPoints` 放会被系统读取、带 id、带数据的生成点。
- `RuntimeGeneratedObjects` 放运行时由 `FieldCreator` 或 manager 生成出来的对象。
- `Managers` 放纯逻辑控制器。

## Demo 点位设计

### 1. Start

用途：

- 玩家出生点。
- 建议放一个能看见火山路线方向的视角。
- 可以用红光、熔岩、远处 Boss 区域当第一眼目标。

需要配置：

- `PlayerStartPoint`
- Field camera 初始角度
- 可选 Tutorial trigger

### 2. Tutorial Gate

用途：

- 只教最基础操作。
- 不要一口气解释所有系统。

建议内容：

- 移动
- 互动键 E
- 背包键 B
- ESC 菜单

需要配置：

- `TutorialData`
- `TutorialTrigger` 或挂在 Field 入口控制器上

### 3. Chest Point

用途：

- 展示 Field 交互和道具获得。
- 给玩家补给，后面战斗能用。

需要配置：

- `FieldChestController`
- `chestId`
- reward item
- visual root / opened visual / closed visual
- E prompt

建议奖励：

- Potion x2
- MP 恢复道具 x1
- Revive item x1

### 4. Slime Encounter

用途：

- 第一场普通战斗。
- 展示 encounter table 驱动。

需要配置：

- `EnemySpawnPoint`
- `spawnId`
- `encounterId`
- `fieldPrefab`
- `EncounterData`
- `EnemyCharacterDataBase`

建议：

- 1 到 2 个 Slime。
- 掉落率可以先设高一点，方便展示 reward UI。

### 5. Recruit Argo

用途：

- 展示入队系统。
- 让玩家在 Boss 前获得第二名角色。

需要配置：

- `FieldRecruitController`
- `recruitId`
- `characterId = Argo_002`
- `visualRoot`
- 可选 `DialogueData`

建议：

- 入队前播放短对话。
- 入队后隐藏 visual root，而不是关闭整个 point。

### 6. Group Encounter

用途：

- 展示联合遇敌。
- 这是项目特色点之一，值得放在 Boss 前。

需要配置：

- 多个 `EnemySpawnPoint`
- 接近的敌人位置
- group encounter 半径
- group line visual
- `EncounterData` 支持多个 enemy entries

建议：

- 两只普通怪靠得比较近。
- 玩家能看到联合线条。
- 进入战斗时弹出 group encounter 提示。

### 7. Boss Gate Dialogue

用途：

- 进入 Boss 前给一点剧情钩子。
- 以后可以升级成 VN 式 StoryCutscenePanel。

第一版：

- 先用现有 `DialoguePanel`
- 或普通 `FieldDialogueController`

之后升级：

- `StoryCutscenePanel`
- 左右立绘
- 角色名
- 表情差分
- 点击 / 空格下一句

### 8. Boss Encounter

用途：

- demo 的核心战斗。
- 展示 boss encounter type 和不同 battle camera performance。

需要配置：

- Boss `EncounterData`
- `encounterType = Boss`
- Boss enemy id
- Boss field prefab
- Boss spawn id
- boss reward

建议：

- Boss 不需要太复杂，但血量和节奏要比普通怪明显。
- 使用 boss camera intro。
- 战斗后显示 reward pop，再显示 level up pop。

### 9. Ending Trigger

用途：

- Boss 战结束后进入 demo 结尾。
- 作为倒叙转场入口。

需要配置：

- `EndingTriggerController`
- 可选 Dialogue / StoryCutscene
- 可选黑屏文字

建议文字：

```text
火山的轰鸣逐渐远去。

三天前。
```

## 第一版不做的东西

这些先放 Backlog，不要抢当前优先级：

- 装备系统
- 完整开放世界
- 复杂 Terrain 雕刻
- 自制角色建模
- 完整 VN 表情动画
- 外部 Excel/DataTable 自动导入
- Boss 多阶段技能设计

## 当前最小完成标准

第一版完成时，应满足：

- New Game 后玩家出现在火山路线起点。
- 玩家可以按路线走到 Boss 区域。
- 路线上至少有一个宝箱、一个普通战斗、一个入队点、一个联合遇敌点、一个 Boss 点、一个 Ending Trigger。
- Save / Load 不破坏玩家位置、宝箱状态、入队状态和已清除敌人状态。
- Boss 战结束后能看到 reward / result，并回到 Field 或进入 ending 流程。

## 推荐 Trello 卡片

### Todo

- Volcano Prologue 灰盒路线
- 火山 demo 点位配置
- Boss Gate Dialogue 第一版
- Ending Trigger 倒叙文字

### Testing

- 从 New Game 到 Ending 可以完整跑通
- Save / Load 后宝箱、入队、敌人清除状态正确
- Group Encounter 可以在火山路线中自然触发
- Boss Encounter 使用 Boss camera performance

### Backlog

- StoryCutscenePanel / VN 式关键剧情
- 火山场景美术 polish
- 装备系统
- 外部表格导入管线
