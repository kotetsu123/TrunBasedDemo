# Adventure of Paul Demo 技术文档（完整中文版）

> 代码与配置快照日期：2026-10-04。
> 完整中文翻译日期：2026-10-05。
> 英文原文：[TECHNICAL_OVERVIEW.md](TECHNICAL_OVERVIEW.md)。

本文件是英文正文的完整中文译文，不是章节概要。章节编号、表格与流程均与原文对应；脚本名、字段名、场景名、资源路径和 ID 保持原样，方便直接在项目中查找。英文版已有的中文概要不在本文件中重复。

## 1. 文档目的与当前范围

本文档描述工作区目前已有的实现，包括 Demo v1 的玩法闭环，以及在此基础上新增的 v2 表现功能。它并不表示每条流程都已经通过了一轮新的回归测试。

本项目是 Unity 回合制 RPG demo，包含两个 Field 探索区域、一个共用的 Battle 战斗场景、跨场景保留的队伍与背包状态，以及 Boss 结局流程：

```text
TitleScene
 -> FildScene：封闭迷宫探索
 -> 宝箱 / 入队 / 对话 / 普通或联合遭遇
 -> BattleScene：回合制战斗
 -> 奖励 / 队伍状态回写 / 返回 Field
 -> Boss_FieldScene：开阔的决战区域
 -> 在 BattleScene 中进行 Boss 战
 -> 返回 Boss_FieldScene
 -> 自动播放结尾对话
 -> DemoEndPanel
 -> 返回 TitleScene
```

表现方向是先经过一个封闭、压抑的火山山洞，再进入开阔的 Boss 战区域，Boss 背后有一座大型火山。已有灰盒和导入的环境资源是朝这个方向推进的步骤，并不是最终美术。

### 依据的不同层级

| 依据 | 能说明什么 |
| --- | --- |
| 玩法代码 | 已实现的职责、逻辑分支和数据流 |
| 序列化的场景与资源配置 | 已保存到磁盘的 Inspector 绑定和参数 |
| `docs/VOLCANO_ROUTE_PLAYTEST.md` | 用户记录的手动测试、备注与未完成用例 |
| `ArtSource/VolcanoCave/` 中的报告 | 之前环境生成与试玩验证的记录 |
| 本次文档更新 | 仅核对代码与配置，没有重新进行 Unity Play Mode 或构建测试 |

“脚本已经存在”“引用已经绑定”和“完整流程已通过端到端测试”是不同的里程碑。描述作品集进度时，应当保留这些区别。

## 2. 开发平台、场景与目录结构

### 技术组成

| 组成部分 | 当前使用方式 |
| --- | --- |
| Unity | `2022.3.62f2` |
| 渲染 | Built-in Render Pipeline；Graphics Settings 中没有启用自定义渲染管线 |
| 输入 | 玩法使用 `UnityEngine.Input`；项目输入处理设置为 Both |
| UI | uGUI、CanvasGroup、TextMeshPro |
| 动画与补间 | 项目中包含 DOTween，用于面板、时间轴和宝箱表现 |
| 配置 | 使用包含可序列化条目的 ScriptableObject 资源 |
| 持久化 | 使用 `JsonUtility`，在 `Application.persistentDataPath` 下读写 JSON 文件 |
| 导航 | 项目中有 AI Navigation 包，但 Field 敌人移动并不由 NavMesh 驱动 |

Build Settings 中启用的场景是 `TitleScene`、`FildScene`、`BattleScene` 和 `Boss_FieldScene`。`FildScene` 是场景实际使用的拼写，场景加载代码必须使用这个名字。

### 代码与资源目录

| 位置 | 内容 |
| --- | --- |
| `Assets/Scripts/Filed/` | Field 创建、移动、镜头、遭遇、背包、交互对象和场景切换 |
| `Assets/Scripts/Battle/` | 战斗流程协调、生成、阵型、目标选择、镜头和教程入口 |
| `Assets/Scripts/Character/Controller/` | 角色效果执行，以及玩家与敌方控制器 |
| `Assets/Scripts/Data/` | 角色、道具、技能、对话、教程、宝箱配置，运行时状态与存档 DTO |
| `Assets/Scripts/UI/` | 战斗和 Field 展示、VN 对话、教程、弹窗和 demo 结局 |
| `Assets/Scripts/Manager/` | 存档系统与角色结算流程 |
| `Assets/Scripts/Environment/` | 火山环境的遮挡可见性处理 |
| `Assets/Scripts/DebugTools/` | 存取档、入队、复活与诊断工具 |
| `Assets/Art/Environment/VolcanoCave/` | Unity 侧环境模型、材质、着色器和 prefab |
| `ArtSource/VolcanoCave/` | 生成源文件、布局、清单、预览和历史报告 |
| `Assets/Editor/VolcanoCave/` | 本地环境导出、构建与试玩验证的编辑器工具 |
| `docs/` | 技术文档、场景配置指南、设计计划和手动测试记录 |

本文保留现有目录名 `Filed` 和 `FiledPartyHudItem` 等类名，以便与代码一致。本次文档更新不会修改这些名字。

## 3. 数据归属与稳定 ID

### 五个不同层次

| 层次 | 示例 | 职责 |
| --- | --- | --- |
| 配置 | `FieldData`、`EncounterData`、`CharacterDataBase`、`EnemyCharacterDataBase`、`ItemDataBase`、`SkillData` | 制作好的模板和查询来源 |
| 跨场景运行时状态 | `PartyRuntimeState`、`InventoryRuntimeState`、`FieldBattleContext`、`TutorialRuntimeState`、`FieldAutoEventRuntimeState` | 当前进度与可变的游戏状态 |
| 战斗副本 | 通过 `Character.Copy()` 创建的 `BaseController.data` | 本场战斗中的 HP/MP、行动值、效果和回合标记 |
| 存档 DTO | `GameSaveData`、`PartySaveData`、`InventorySaveData`、`FieldSaveData` | 可序列化的数值与 ID，不包含场景或组件引用 |
| 展示层 | HUD、时间轴图标、弹窗、对话面板 | 展示其他层提供的状态 |

`Character` 是一个可序列化的 C# 类，本身并不是 ScriptableObject 资源。角色数据库资源中保存的是这些模板的列表。

战斗生成、入队和读档会使用角色副本。不过，`PartyRuntimeState.InitializeIfEmpty` 目前会直接把传入列表中的初始成员加入队伍。因此，不能把所有初始化过程都视为已经完成深拷贝的边界。

### ID 对照表

| ID | 含义 | 需要区分的地方 |
| --- | --- | --- |
| `fieldId` | FieldData 资源上的区域身份信息 | 当前加载场景使用场景名，并没有通过 fieldId 注册表加载 |
| `spawnId` | 一个手工放置或动态生成的敌人遭遇点 | 用于清除状态、刷新时间戳和结局条件 |
| `encounterId` | 一份战斗队伍与奖励配置 | 多个生成点可以引用同一个遭遇 |
| EnemyEntries 中的 `enemyId` | 查询敌人模板的键 | 对应 EnemyCharacterDataBase 中的 `Character.characterId` |
| `characterId` | 队伍成员或敌人模板的身份 | 用于队伍匹配、入队和存档恢复 |
| `itemId` | 查询道具模板的键 | 存档保存它，而不是保存 ItemData 引用 |
| `objectId` | 动态生成的 Field 对象身份 | 传给 FieldChestController，作为运行时宝箱 ID |
| FieldChestController 上的 `chestId` | 宝箱已开启状态的持久化键 | 必须稳定，并且在不同 Field 场景间也保持唯一 |
| ChestRewardData 上的 `chestId` | 奖励资源的身份信息 | 不会独立覆盖控制器使用的持久化键 |
| `recruitId` | 入队点的身份与配置 | 当前是否已入队由队伍中是否包含该角色决定 |
| 场景落点 ID | 一个命名的到达位置，例如 `FromMaze` | 不是敌人的 spawnId，也不负责选择遭遇 |
| `tutorialId` / 自动事件 ID | 一次性教程或事件的完成键 | 分别保存在各自的运行时完成集合中 |

需要持久化的生成点、宝箱和事件 ID 应当在不同区域间保持全局唯一。当前完成集合没有按场景或 fieldId 再嵌套一层命名空间。

### 查询数据库

- `CharacterDataBase`：队伍角色模板；还保留通过绑定的初始队伍数据进行兼容查询的入口。
- `EnemyCharacterDataBase`：敌人模板，包括属性、头像、技能、AI 类型与技能权重。
- `ItemDataBase`：把 itemId 解析为 ItemData。
- `EncounterDataBase`：把 encounterId 解析为 EncounterData。
- `EnemyDataBase` / `EnemyFieldData`：为了兼容而保留的旧 Field 查询路径。

数据库负责说明“这个 ID 代表什么”。它不保存玩家当前持有多少道具，也不会在保存时取代运行时队伍。

## 4. Field 启动、动态生成与玩家落点

主要代码：`FieldCreator.cs`、`FieldData.cs`、`FieldSaveContext.cs`、`EnemySpawnManager.cs`。

### 启动职责

`FieldCreator.Start` 执行以下工作：

1. 清除 Field 暂停状态。
2. 仅在运行时状态尚未初始化时，初始化队伍和背包。
3. 根据 FieldData 生成敌人生成点标记。
4. 生成配置的对象与入队点。
5. 根据适用的战斗返回、场景到达、读档或默认位置摆放玩家。
6. 请求 EnemySpawnManager 生成遭遇敌人。
7. 刷新队伍 HUD。
8. 从 Battle 返回时启动遇敌冷却，并清除临时返回数据。

初始背包不会在每次进入场景时重复添加。已经初始化过的空背包会继续保持为空。

### FieldData 条目

| 条目 | 主要配置 |
| --- | --- |
| FieldSpawnPointEntry | spawnId、encounterId、Field prefab、位置、欧拉角旋转、游荡半径、固定不动标记、刷新类型与时间、旧 enemyId |
| FieldObjectEntry | objectId、prefab、宝箱奖励覆盖配置、位置、欧拉角旋转、缩放 |
| FieldRecruitPointEntry | recruitId、characterId、逻辑点 prefab、视觉 prefab、变换、提示、可选入队前对话、入队后禁用 |

FieldData 是玩法点的布局与配置来源。目前它不是地形生成器、程序化迷宫引擎，也不是整个场景的替代品。

### 动态生成根节点

| FieldCreator 字段 | 预期存放内容 | 未绑定时的回退位置 |
| --- | --- | --- |
| `generatedSpawnPointRoot` | 动态生成的敌人生成点标记 | FieldCreator 的 transform |
| `generatedInteractableRoot` | 宝箱与入队点 | 生成点根节点 |
| `generatedEnvironmentRoot` | 其他动态生成的环境对象 | FieldCreator 的 transform |

当前 Field 场景绑定了生成点和交互对象的根节点。大型手工摆放的环境模型与墙壁可以独立保留在 `Environment` 下。

如果希望管理器下不挂动态生成的子物体，应明确绑定各个根节点。EnemySpawnManager 实际实例化的敌人不会自动成为生成点标记根节点的子物体。

当 FieldData 提供动态生成点标记时，FieldCreator 会把它们传给 EnemySpawnManager。不使用 FieldData 时，仍可以走原有的场景生成点配置。对象或入队条目的缩放如果整体为零，会发出警告并修正为 `Vector3.one`。

### 玩家落点优先级

```text
战斗返回的变换
 -> 待处理的 Field 到 Field 命名落点
 -> 待应用的存档玩家变换
 -> FieldCreator.playerStartPoint
```

`FieldPlayerTransformUtility.Teleport` 会同步 Rigidbody 的位置与旋转、清除速度、更新 Transform，并同步物理系统。这能避免读档或传送后，Rigidbody 马上把角色恢复到旧位置。

## 5. Field 场景之间的切换

主要代码：`FieldSceneTransitionTrigger.cs`、`FieldSceneSpawnPoint.cs`、`FieldSceneTransitionContext.cs`、`SceneTransitionController.cs`。

一次场景切换涉及两个独立对象：

- 来源区域中的 Trigger 指定目标场景和目标落点 ID。
- 目标区域中的 FieldSceneSpawnPoint 提供对应的位置与旋转。

```text
玩家进入场景切换 Trigger
 -> 保存目标场景和目标落点 ID
 -> 渐变 / 加载场景
 -> 目标场景的 FieldCreator 取出并处理到达请求
 -> 查找匹配的 FieldSceneSpawnPoint
 -> 把玩家传送到那里
```

例如，`FromMaze` 表示“从迷宫过来时使用的到达点”。它只是代码用来匹配的标签，不是 Unity 保留关键字。

Boss_FieldScene 中的返回 Trigger 可以指向 FildScene 中另一个命名落点。到达点应放在目标 Trigger 的范围外，避免刚到达就立刻被传回去。

FieldCreator 的默认 playerStartPoint 仍用于 New Game，以及没有指定命名目标的位置入口；但它不是唯一可用的落点。

进入 Battle 也使用场景渐变流程。切换期间必须停止 Field 移动，避免画面渐变时角色仍在互相顶撞。

## 6. Field 敌人状态机、视线与刷新

主要代码：`EnemyFieldController.cs`、`EnemySpawnPoint.cs`、`EnemySpawnManager.cs`、`FieldBattleContext.cs`。

### 移动状态机

Field 敌人现在在 Update 中使用 `Wander` 和 `Chase` 状态，替代了之前的移动协程结构。

| 状态或条件 | 行为 |
| --- | --- |
| Wander | 在出生原点附近的 XZ 圆形区域内随机选点，走过去，然后等待 |
| Chase | 在检测和距离条件仍然有效时朝玩家移动 |
| Field 暂停 | 不更新敌人移动 |
| `isStationary` | 跳过游荡和追踪移动，但保留遭遇功能 |

随机采样使用 Vector2，是因为游荡区域是一个水平圆盘。采样的两个分量会映射到世界坐标 X/Z。

移动仍然是直接修改 Transform。卡住检测会定期检查移动进展；如果移动不足，就重新选择游荡目标。这能减少持续顶墙，但不会计算绕墙到达目标的可通行路径。

### 视线检测

追踪检测结合距离和抬高后的 `Physics.Linecast`。障碍物 LayerMask 应包含迷宫的实体墙；视线检测会忽略 Trigger Collider。

障碍物 mask 为零时，会保留旧的仅按距离判断行为。安装了导航包，并不会自动开启寻路或隔墙检测。

### 持久化生成点状态

`FieldBattleContext` 保存：

- HashSet 中的已清除生成点 ID。
- 字典中的清除 UTC 时间戳。
- 临时战斗返回变换与遭遇列表。
- 战斗结束后的短暂遇敌冷却。

HashSet 用于回答“这个生成点是否已经被清除”，同时避免重复加入相同 ID。时间戳字典用于回答“它是什么时候被清除的”。

| 刷新类型 | 胜利后的行为 |
| --- | --- |
| Permanent | 持续跳过已经清除的生成点 |
| Timed | 在配置的时长过去之前跳过生成 |
| Timed 且延迟不为正 | 清除记录，允许生成 |

如果某个已清除的定时刷新点没有时间戳，会把当前 UTC 时间作为起点。UTC 时间戳以可往返还原的字符串格式写入存档 DTO。

定时刷新使用现实世界的 UTC 时间，因此经过的时间可以包含退出游戏后的时间。Field 场景运行时，EnemySpawnManager 也会按可配置间隔检查定时刷新点，不再必须重载场景才能刷新。

`HasActiveEnemy` 防止一个已经登记了存活敌人的 spawnId 再次生成敌人。`RegisterActiveEnemy` 维护这项对应关系。

### 战斗后冷却是另一回事

短暂遇敌冷却使用 `Time.time`，而不是 UTC。它防止从 Battle 返回后马上再次进入遭遇，包括逃跑返回的情况。它不是无敌或伤害系统，本身也不会阻止敌人继续追踪或从物理上挡住玩家。

## 7. 联合遭遇与战斗增援

主要代码：`EncounterTrigger.cs`、`FieldBattleContext.cs`、`BattleSpawner.cs`、`BattleFormation.cs`。

### Field 侧的收集

当来源敌人处于追踪状态时，它的遭遇 Trigger 可以收集配置的水平半径内、当前处于活动状态的附近敌人。可选的视线检测会排除被实体墙与来源敌人隔开的同伴。

收集以来源敌人为中心，只检查一层邻近关系：不会从每个新加入的同伴继续向外递归扩张。附近候选敌人也不需要全部处于 Chase。

```text
来源敌人碰到玩家
 -> 暂停 / 已触发 / 处于遇敌冷却时拒绝触发
 -> 收集符合条件的附近敌人遭遇点
 -> 在 FieldBattleContext 中记录 spawn ID 和 encounter ID
 -> 加载 BattleScene
```

Spawn ID 会去重。Encounter ID 则有意保留重复条目：两个不同的 Field 敌人即使使用相同遭遇配置，也仍然贡献两支队伍。

胜利会把所有参与的 spawn ID 标记为已清除，而不是只标记最初触发的那个点。

### 可见的联合连线

EncounterTrigger 管理复用的运行时 LineRenderer，用于显示附近联合成员之间的连线。成员关系按间隔检测，线的端点位置在 LateUpdate 中刷新，以跟随移动中的角色。

这些线在游戏中可见，与只在编辑器中显示的 Gizmo 不同。优先使用绑定的材质；没有绑定时会创建运行时回退材质。

### Battle 侧的队列

BattleSpawner 解析每个参与的遭遇，并合并得到的敌人生成请求。BattleFormation 的当前上场敌人槽位有限，默认是五个。

超过槽位数量的敌人在队列中等待。释放槽位后，下一个敌人会被实例化、注册，并加入时间轴。还在队列中等待的敌人，不是已经可以行动的时间轴参与者。

当前默认玩家阵型有四个槽位。如果增加队伍人数，需要同步处理生成、UI 和阵型容量；入队逻辑本身不会强制这个人数上限。

## 8. 遭遇表与战斗生成

主要代码：`EncounterData.cs`、`EncounterDataBase.cs`、`EnemyCharacterDataBase.cs`、`BattleSpawner.cs`。

### 首选数据流

```text
FieldData.spawnPoints
 -> EnemySpawnPoint：spawnId + encounterId + Field prefab
 -> EncounterTrigger
 -> FieldBattleContext.CurrentEncounterIds
 -> 查询 EncounterDataBase
 -> EncounterData.EnemyEntries：enemyId + count
 -> 查询 EnemyCharacterDataBase
 -> Character.Copy()
 -> 战斗敌人 prefab + 解析出的角色数据
```

Field prefab 表示探索场景中的遭遇对象。EncounterData 中的敌人模板和数量，决定进入 Battle 后实际出现什么。

EncounterEnemyEntry 使用 `enemyId`，数据库则把它与敌方 Character 的 `characterId` 匹配。这两个名字是在不同上下文中描述同一个查询键，不是两个无关的身份。

### 兼容路径

| 情况 | 当前行为 |
| --- | --- |
| EnemyEntries 有效 | 按 ID 解析模板，并按各自 count 生成对应数量 |
| 某个遭遇没有解析出任何敌人，且允许旧数据回退 | 使用该遭遇内序列化的旧敌人角色列表 |
| 整场战斗没有解析出任何敌人 | 使用配置的初始战斗敌人 |
| 旧 Field 生成配置缺少数据，且存在旧 enemyId | 尝试较早的 EnemyDataBase 查询路径 |

回退不一定会为每条错误记录分别补一个替代对象。部分有效的 EncounterData 可能只生成成功解析的条目。因此，验证是否走了预期表路径时，查询与来源日志很重要。

遭遇配置校验会报告空 ID、无效条目和缺少掉落道具。它属于诊断校验，并不是发现任意错误就整份拒绝执行的事务式校验。

### 玩家生成

战斗玩家来自 PartyRuntimeState，并会复制一份用于本场战斗。序列化的玩家 prefab 条目提供场景对象，不是当前队伍 HP/MP 和成长进度的数据来源。

直接启动 BattleScene，需要运行时队伍已经初始化，例如通过合适的调试入口。BattleSpawner 不会根据玩家 prefab 自动创建正常的初始队伍。

头像优先来自解析出的 Character 数据。只有角色没有头像时，prefab 控制器上的头像才作为回退。HUD、结算和时间轴的初始化应当使用同一个已经解析的数据来源。

## 9. 战斗流程协调与行动值时间轴

主要代码：`BattleManager.cs`、`BattleTargetSelector.cs`、`TimeLineUI.cs`、`TimelineIconView.cs`。

BattleManager 仍然是核心流程协调者。它协调生成、战斗就绪、回合推进、指令、目标选择、技能执行、镜头移动、死亡与复活、奖励，以及结算事件。

重要事件包括 `OnTimeLineOrdered`、`OnCurrentActorChanged`、`OnTargetChanged`、`OnInputStateChanged`、`OnBattleEnded` 和背包数量变化事件。

### 回合推进

战斗已经就绪、尚未结束，而且没有正在执行的行动时：

```text
ActionValue -= Speed * (10 / 0.75) * Time.deltaTime
```

一次行动完成后，该行动者的行动值会重置到最大值。行动流程执行期间，暂停递减行动值。

对于尚未准备好行动的角色，时间轴排序使用剩余行动值相对于 Speed 的结果。已经达到行动条件的角色使用另一条排序路径。无效、死亡或不在场的角色不会参与有效的行动预测。

LateUpdate 请求更新排序，并且只在顺序变化时发布新结果。UI 补间把图标移动到对应槽位。时间轴是当前调度状态的预测展示，不是另一套独立回合逻辑的队列。

### 输入与镜头同步

Attack、Skill、Item 和 Run 是指令入口。敌方目标选择支持键盘切换和鼠标选择；友方与死亡友方目标有各自的路径，不能默认它们与敌方目标选择完全相同。

战斗镜头移动时，确认输入会被阻止。等待镜头的协程分支会 yield 交还控制权，而不是执行紧密的忙循环，避免过早确认导致 Editor 卡死。

### 死亡与复活

- 死亡队员仍然属于队伍，也可以继续作为复活目标。
- 以 HP 为零的状态进入新战斗，不会自动被治疗。
- 生成时，死亡队员不会得到活动的时间轴图标。
- 死亡会移除活动图标；复活会启用控制器、重建缺失图标，并请求刷新时间轴。
- 敌人死亡会释放阵型槽位，让队列中的增援可以入场。

队伍和结算顺序按照运行时队伍的稳定身份与原有顺序确定，不依赖控制器死亡或重新加入的先后顺序。

## 10. 技能、敌方 AI 权重与冷却

主要代码：`Character.cs`、`SkillData.cs`、`EnemyAiType.cs`、`BaseController.cs`、`BattleManager.cs`。

### 技能来源与效果执行

角色数据中的技能列表是首选来源。角色列表为空时，控制器上的技能配置仍然作为兼容来源。

BaseController 把技能启动与效果应用分开：

- 验证并支付一次 MP 消耗。
- 提示一次技能名称。
- 对一个目标或一组目标应用伤害、治疗或复活。
- 完成一次技能使用。

群体执行不会对每个目标重复支付 MP，也不会重复提示同一个技能。当前伤害使用 Attack 加上技能 power；治疗使用配置的 power。伤害、治疗和 MP 飘字仍需要绑定对应的浮动文字 prefab。

敌方 `EnemyAll` 行动路径会选择对面的存活玩家群体，不再错误地进入默认自身目标分支。玩家侧群体目标技能流程，与这套敌方实现还不完全等价。

### 普通敌人与 Boss AI

`Character.enemyAiType` 控制敌方决策策略：

- 普通敌人保留低 HP 时优先治疗的规则，同时受治疗次数与冷却条件限制。
- Boss 跳过通用的低 HP 治疗优先规则，使用配置的伤害技能候选集。
- 伤害候选技能必须先通过冷却检查，才能进入加权选择。

这个 AI 类型与 EncounterData 的开场镜头类型、Field 固定不动标记是独立的配置。

### 加权选择

每个 EnemySkillWeightEntry 引用一个 SkillData 和一个整数权重。没有单独配置的技能，默认权重为 1。

例如 Fire 权重为 3，MagmaBurst 权重为 1：

```text
总权重 = 4
随机整数 = 0、1、2 或 3

Fire 的累计区间：       [0, 3)
MagmaBurst 的累计区间： [3, 4)
```

在这两个技能都符合选择条件时，MagmaBurst 的概率是 1/4。这不保证每四个回合一定释放一次。

只要候选技能中存在正权重，权重为零的技能就不会被选中。如果所有符合条件的权重都是零，当前代码会警告并回退到等概率选择。因此，零不是一个无条件禁用技能的开关。

### 敌方冷却回合

`SkillData.enemyCooldownTurns` 表示：这个敌人之后的多少个自身回合中，该技能不可用。

BattleManager 按每个行动者、每个 SkillData 保存冷却。该敌人回合结束后先推进已有冷却，再登记刚使用技能的新冷却。冷却为二，就会阻止接下来的两个自身回合使用该技能。

这不是秒数计时器、全队共享冷却、存档字段，也不代表已经完成玩家技能冷却系统。

当前选择逻辑按冷却筛选，但没有完整地提前筛掉 MP 不足的技能。技能可能到执行时才被拒绝；冷却登记也不是根据一个返回的“效果成功”结果决定。这是后续可以改进的边界。

## 11. Field 与战斗镜头表现

### 共用的 Field 默认配置

主要代码：`FieldCameraSettings.cs`、`FieldCameraController.cs`。

共享配置资源是：

`Assets/Scripts/Data/FieldData/CameraData/FieldCameraSettings_Default.asset`

当前两个 Field 场景都引用这个资源。FieldCameraController 在 Awake 中读取它，并应用跟随与旋转的默认值：

| 设置 | 共享资源中的值 |
| --- | --- |
| Distance | 6 |
| Height | 3 |
| Smooth Time | 0.05 |
| Default Pitch | -12.5 |
| Rotate Speed | 3 |
| Min / Max Pitch | -15 / 60 |

Yaw 仍然由场景单独决定，让每个场景入口可以朝向适合自己的构图方向。没有绑定配置资源时，继续使用控制器上序列化的值。

启动时，资源会覆盖控制器上对应的字段。它不是实时广播的设置，也不会把玩家当前镜头角度跨场景保存。

Pitch 会旋转跟随偏移，然后镜头朝向玩家上方的注视点。因此，Inspector 中的 pitch 并不直接等于 Camera Transform 最终的欧拉角。

FieldPauseState 暂停时，会阻止 Field 旋转输入；跟随更新仍可以继续。除非两个驱动已明确协调，否则不要在同一相机上同时启用第二个跟随脚本。

### 战斗开场与行动

`EncounterData.introCameraType` 选择 Normal 或 Boss 表现。联合遭遇中只要有一个遭遇使用 Boss 开场，就会选择 Boss 表现。

- Normal：使用下一个友方行动者和默认存活敌方目标，建立对峙镜头。
- Boss：播放配置的战斗开场演出，再建立对峙镜头。
- 敌方群体技能：先展示施放者，等待，再移动到玩家群体镜头，最后执行效果。

BattleCameraDirector 管理镜头移动协程，并把移动状态提供给指令流程。这使镜头插值与回合推进分开，不过大量流程协调仍在 BattleManager 中。

### 火山环境遮挡淡化

`VolcanoCaveVisibility` 使用 shader 的 `_Fade` 属性和 MaterialPropertyBlock，让配置的 Renderer 在挡住相机到玩家的视线时淡化。

它不会禁用 Collider，也不会让墙在玩法射线检测中变得透明。淡化后的墙仍能阻挡移动、敌人视线和联合遭遇。

这是一套依赖 Renderer 列表与特定 shader 的方案，不会自动让每一种导入材质都支持透明化。

## 12. 奖励、结算与返回流程

主要代码：`EncounterData.cs`、`BattleManager.cs`、`BattleEndPanelController.cs`、`ResultCharacterPanelController.cs`、`RewardPopController.cs`、`LevelUpPopController.cs`。

### 奖励归属

EncounterData 定义奖励 EXP 和独立的道具掉落条目。EncounterRewardService 计算并发放奖励，返回 EncounterRewardResult，其中包含 EXP、实际获得的道具和升级结果。

对于联合遭遇：

- 汇总成功解析的遭遇列表中的奖励 EXP。
- 重复的遭遇条目也会重复贡献奖励。
- 每个掉落条目独立随机判定。
- 成功掉落的道具在弹窗出现之前就加入 InventoryRuntimeState。

当前掉落判断在 `Random.value > dropChance` 时拒绝掉落。数量最少会被保护为一；整数最大值通过 `Random.Range(min, max + 1)` 包含在可能结果中。

每个符合条件的玩家控制器都会得到计算出的完整 EXP，不会再由队伍平分。没有可用 EncounterData 奖励来源时，仍保留 120 EXP 的旧回退值。

### 展示顺序

```text
BattleManager 确定结果并创建 payload
 -> BattleEndPanelController 显示 Win / Lose / Escape
 -> 关闭该面板时发出 OnClosed(payload)
 -> ResultCharacterPanelController 显示角色结算

Win：
 -> RewardPopController
 -> 为升级成员显示 LevelUpPopController
 -> 等待返回延迟
 -> 加载原来的 Field 场景

Escape：
 -> 显示角色结算
 -> 等待返回延迟
 -> 加载原来的 Field 场景

Lose：
 -> 失败按钮
 -> Retry / 返回标题 / 可选的已配置 Load
```

奖励弹窗与升级流程由 ResultCharacterPanelController 在结束面板关闭后协调，不是直接由 BattleManager 播放。

RewardPopController 显示 EXP 和实际获得的道具行。没有道具掉落时，只显示 EXP；当前不会额外增加“没有掉落道具”的一行。EXP 不为正、也没有道具时，会跳过奖励弹窗。

展示之前，奖励就已经实际发放到玩法状态中。跳过或缺少弹窗，本身不会取消已经发放的奖励。

### 回写与重试

Win 和 Escape 会把本场战斗的当前队伍状态写回 PartyRuntimeState。Lose 不执行这项回写，因此 Retry 可以重建战斗前的队伍。

背包是实时共享状态：Retry 不会自动回退已经消耗的道具。“Retry 恢复队伍”不能被描述成完整的事务回滚。

结算面板启动时使用 HideImmediate，避免进入新 BattleScene 时短暂显示一帧。

## 13. 背包结构、拖拽与道具使用

主要代码：`InventoryRuntimeState.cs`、`InventorySlot.cs`、`DraggableItem.cs`、`FieldInventoryPanelController.cs`、`FieldInventoryPartyTargetPanelController.cs`、`ItemData.cs`。

### 有序运行时格子

InventoryRuntimeState 保存有顺序的格子记录，每个记录包含 ItemData 和数量。

- 默认最低容量为 20。
- 空格子也是实际存在的条目，只是没有道具和数量。
- 添加已经存在的道具时，会堆叠到它当前所在的格子。
- 否则使用空格子；没有空格时扩充容量。
- 消耗最后一个道具后，留下空格，而不是移除这个格子。
- 交换只改变格子的内容，不会让后面格子的索引整体移动。

目前没有硬容量限制、最大堆叠数量、装备格子模型，也没有外部道具表导入器。

### 拖拽层级

```text
ItemSlot                         固定网格格子 / 放置目标
 -> ItemRoot                     DraggableItem
     -> ItemIcon
     -> CountText
```

拖拽时只移动 ItemRoot。移动网格格子本身可能干扰布局顺序，并导致非预期的格子索引。

DraggableItem 从 FieldInventoryItemView 获取来源索引。InventorySlot 获取目标索引，交换运行时数据，并请求面板刷新。Icon 和 count 共享同一个可拖拽父物体，因此会一起移动。

空格展示会隐藏 icon/count，但不删除底层运行时格子。

### Field 使用流程

```text
选择背包道具
 -> 显示说明
 -> Use
 -> 打开独立的队伍目标面板
 -> 选择成员
 -> 验证道具和目标
 -> 有效时应用效果并消耗道具
 -> 刷新背包和队伍 HUD
```

目标面板和普通 Field 队伍 HUD 使用同一种展示组件，但它们是不同的对象实例，也使用不同的绑定方式。只有目标选择 HUD 需要可点击的目标回调。

目标无效时，可以禁用 Use 按钮。选择满 HP、满 MP 或其他无效成员时，可以显示 toast 解释原因；验证失败不会消耗道具。

| ItemType 数值 | 已实现的含义 |
| --- | --- |
| None = 0 | 没有可使用的效果 |
| Heal = 1 | 恢复存活目标的 HP |
| RestoreMp = 2 | 恢复 MP |
| Revive = 3 | 复活死亡成员 |
| Buff = 4 | 预留接口与类型，尚未完整实现 buff 道具 |

明确指定 enum 数值，可以在增加新分类时保留已有序列化道具类型的含义。

Battle 道具选择也使用这份共享运行时背包。目标和效果校验支持恢复与复活；每次进入战斗不会重新填入已经消耗的道具。

## 14. 队伍状态、成长、入队与调试工具

主要代码：`PartyRuntimeState.cs`、`Character.cs`、`CharacterDataBase.cs`、`FieldRecruitController.cs`、`PartyRecruitDebugTester.cs`。

### 队伍与成长

PartyRuntimeState 按顺序保存队伍，也包含死亡成员。回写按稳定 character ID 匹配，并保留按名称匹配的兼容回退，同时维持原有队伍顺序。

角色成长目前包括：

| 规则 | 当前实现 |
| --- | --- |
| 升到下一级所需 EXP | Level * 100 |
| 每级最大 HP / MP | +5 / +5 |
| 每级 Attack / Speed | +10 / +0.5 |
| 升级时 | 补满 HP 和 MP |

因此，成长并不局限于 HP 和 MP。如何正确保存所有成长后的属性是另一个问题，见持久化限制部分。

### 逻辑点与视觉子节点

```text
RecruitPoint
 - FieldRecruitController
 - Trigger Collider
 -> VisualRoot
     -> 角色模型 / 动画表现
```

父节点负责交互与数据。子节点展示可以入队的角色。入队时隐藏 VisualRoot，仍保留逻辑点，以便后续刷新或读档回退。

FieldCreator 可以通过 FieldRecruitPointEntry 配置通用入队点 prefab。按 characterId 解析角色模板、复制，并在队伍中尚不存在该角色时追加进去。

生成点初始化时，会禁用旧角色 prefab 上嵌套的入队控制器和 Collider，避免重复管理交互。新内容应优先使用只负责表现的视觉 prefab。

可选的入队前 DialogueData 会在完成入队行动之前播放。当前入队使用普通对话面板路径。

是否已入队由 PartyRuntimeState 中是否存在该 characterId 决定，不是由一个独立保存的入队布尔值决定。读取入队前的存档后，会根据恢复的队伍刷新入队点可见性。

交互中的玩家判断通过 Tag 或组件层级识别正在交互的玩家，并不表示每个可入队的角色模板自己都必须挂 SimplePlayerMovement。

### 调试入队

PartyRecruitDebugTester 可以直接加入配置的角色并刷新队伍 HUD。它不需要模拟与入队点交互，也不需要手动隐藏入队点。

入队目前没有实现离队、换队、人数上限策略，也不会自动创建无限数量的 HUD 条目。

## 15. 宝箱、交互提示与 Toast

主要代码：`ChestRewardData.cs`、`FieldChestController.cs`、`FieldInteractionPromptController.cs`、`FieldToastController.cs`、`FieldData.cs`。

### 数据驱动的宝箱奖励

```text
FieldObjectEntry
 - objectId
 - 共用的宝箱 prefab
 - chestRewardData 覆盖配置
 -> FieldCreator 实例化 prefab
 -> FieldChestController.Configure(objectId, rewardData)
 -> E 交互
 -> 向 InventoryRuntimeState 发放道具
 -> 记录已开启宝箱 ID
 -> 显示 toast / 开箱视觉效果
```

奖励来源优先使用条目传入的奖励数据，其次是 prefab 自带的奖励数据，最后是旧的内置奖励列表。这让多个宝箱可以共用同一个 prefab，却发放不同的配置奖励。

宝箱奖励目前是固定的道具与数量列表。遭遇掉落有概率判定，但宝箱不会自动继承那套随机判定系统。Gold 还不是一套已经完成的共享运行时奖励或货币系统。

### 碰撞与视觉

宝箱交互 Trigger 和实体阻挡 Collider 负责不同事情。开启前，阻挡 Collider 防止玩家穿过宝箱。开启后，禁用阻挡，并更新关闭与开启的视觉物体，或给绑定的盖子播放动画。

ClosedVisual 和 OpenedVisual 是两种显示状态的引用，不是道具奖励数据。盖子动画是可选的，取决于提供的模型层级。

已开启 ID 通过 FieldBattleContext 及其存档快照保留。新生成的宝箱在启动时应用开启状态。目前同场景 Load 不会专门遍历所有现有宝箱进行刷新；仅有 DTO，并不能保证视觉状态也一起回退。

### 共用 UI

- FieldInteractionPromptController 为当前交互所属对象显示 E 或按键提示。如果只想显示按键图标，可以不绑定可选的提示文字。
- Prompt 直接使用 CanvasGroup，不是 BasePanel 子类。
- FieldToastController 继承 BasePanel，显示短暂、不可交互的消息。
- 宝箱、入队、背包使用失败，以及菜单存取档路径会调用 toast 展示器，并传入消息。
- 新 toast 会替换当前消息并重置显示时长；目前没有消息队列。

消息的产生方知道玩法结果，ToastController 只负责展示这条消息。

## 16. 普通对话与 VN 表现

主要代码：`DialogueData.cs`、`DialoguePanelController.cs`、`VNDialoguePanelController.cs`、`FieldDialogueController.cs`。

### 共用对话数据

DialogueData 包含：

- 一个 dialogueId 和有顺序的对白行。
- 为整段对话提供的一张可选背景 Sprite。
- 打字效果开关和每秒字符数设置。

每行包含说话者 ID、名字、头像 Sprite、头像所在侧和对白文字。说话者 ID 是身份信息；当前面板不会自动从 CharacterDataBase 解析表情或头像。

### 两种展示器

| 展示器 | 当前行为 |
| --- | --- |
| DialoguePanelController | 已有的紧凑对话 UI，可选 Next 按钮，鼠标或键盘推进 |
| VNDialoguePanelController | 背景图、左与右头像槽位、说话者与文字显示，无 Next 按钮，通过鼠标或键盘推进 |

VN 的头像侧配置决定使用对应槽位。另一侧已经显示的头像可以保留并调暗，而不是把每个说话者都重新放到左边。

DialogueData 没有背景 Sprite 时，会隐藏绑定的 VN 背景 Image。如果没有绑定 Image 引用，脚本就无法修改背景图。独立的 DimOverlay 仍只是 UI 图层，不是摄像机模糊。

### 可选打字效果

打字效果按整份 DialogueData 配置，不是每行独立配置。它通过控制可见字符逐步显示：

```text
文字还在逐字显示时推进 -> 显示当前整行
当前行已经显示完时推进 -> 显示下一行
```

关闭开关后，文字会立即显示。打字计时使用不受时间缩放影响的时间。

对话完成会调用发起方提供的回调。Escape 当前也会结束或关闭对话，因此跳过对话时，自动事件也可以继续走完成路径。

这个基础展示器尚未实现分支选项、逐行背景切换、表情数据库、语音播放或完整过场时间线。

## 17. Boss 结局事件与 Demo 完成

主要代码：`FieldAutoDialogueEventController.cs`、`FieldAutoEventRuntimeState.cs`、`DemoEndController.cs`、`FieldEndingController.cs`。

### 战斗后自动流程

```text
Boss 遭遇胜利
 -> 把参与的 spawnId 标记为已清除
 -> 返回 Boss_FieldScene
 -> FieldAutoDialogueEventController.Start 检查条件
 -> 播放绑定的普通或 VN 对话
 -> 对话完成
 -> 把事件标记为完成
 -> 调用 onDialogueFinished
 -> DemoEndController.ShowDemoEnd
```

条件使用的是已经清除的 Field spawnId，不是敌人模板 ID。即使 Boss 点由 FieldCreator 动态生成，仍然有效，因为胜利会把同一个 spawnId 写入 FieldBattleContext。

当前 Boss 配置使用：

| 配置 | 当前值 |
| --- | --- |
| Boss spawnId | `boss_spawn_001` |
| Encounter ID | `encounter_boss_001` |
| 敌人模板 ID | `boss_001` |
| 刷新 / 移动 | Permanent / stationary |
| 自动事件 ID | `boss_ending_001` |
| 要求已清除的 spawn ID | `boss_spawn_001` |
| 展示器 | VN 对话面板 |
| 完成监听方法 | DemoEndController.ShowDemoEnd |

控制器在场景启动时检查，不是每帧持续监控。选择 VN 路径时，必须有 VN 面板引用。缺少所选展示器会发出警告，不会静默切换到普通面板。

FieldAutoEventRuntimeState 用 HashSet 保存已完成 ID，并序列化为列表。对应的 FieldAutoEventSaveData 类目前也在同一个源文件中。

### 结局面板

DemoEndController 在初始时隐藏面板，显示结局时暂停 Field，并提供返回标题行为。对话完成后，它会重新接管暂停，因为对话关闭时先清除了自己设置的暂停。

BasePanel 当前是具体的 MonoBehaviour 类，不是抽象类。在这个实现中，可以用它作为简单结局面板的展示器。

FieldEndingController 是另外一个可选的交互 Trigger 结局路径。Boss 自动对话流程不需要先依赖它才能运行。

## 18. 教程、暂停状态与输入归属

主要代码：`TutorialData.cs`、`FieldTutorialController.cs`、`BattleTutorialController.cs`、`TutorialPanelController.cs`、`TutorialRuntimeState.cs`、`FieldPauseState.cs`。

TutorialData 当前描述按顺序排列的标题与说明页。它是基础说明流程，不是会等待玩家执行 Attack 或选择指定目标的行动条件式教程。

Field 和 Battle 的入口控制器，会在配置的延迟后启动尚未完成的教程。已完成 ID 会阻止重复播放。Field 入口还会在战斗返回或冷却期间避开普通启动教程路径。

面板支持鼠标或键盘推进，以及跳过确认 UI。确认跳过也使用完成回调，把教程标记为已完成。缺少确认窗口根节点时，有警告与回退路径。

明确的输入防护会阻止：

- 原本用于教程的 Escape 同时打开 Field ESC 菜单。
- 教程活动期间执行战斗指令按钮。
- 同一次 Escape 在关闭背包后又打开 ESC 菜单。

### 暂停是合作式标记

FieldPauseState 当前封装一个 bool，不是暂停令牌栈，也不会设置 Time.timeScale。

使用方必须主动检查它。Field 移动、敌人移动和镜头旋转都有暂停处理，但它不会自动暂停所有系统：

- Field 镜头跟随可能继续。
- 定时生成检查没有通用的 FieldPauseState 防护。
- 基于 UTC 的刷新时间继续经过。
- 仅靠 FieldPauseState，不会整体冻结 BattleManager 中行动值的 Update。

CanvasGroup 的隐藏、可交互与射线设置同样重要。教程不可见但控制器仍持有暂停时，可能看起来像玩法卡死。

多个模态界面重叠时需要注意：一个控制器清除共享 bool，可能意外解除另一个窗口的暂停。使用令牌或引用计数的暂停服务是未来可改进的方向，不属于当前实现。

## 19. 存取档快照与恢复

主要代码：`SaveSystem.cs`、`GameSaveData.cs`、`InventorySaveData.cs`、`PartyMemberSaveData.cs`、`FieldSaveData.cs`、`TutorialSaveData.cs`。

### 存档内容

SaveSystem 构建快照，并写入：

`Path.Combine(Application.persistentDataPath, "save.json")`

| 快照 | 保存的内容 |
| --- | --- |
| GameSaveData | 版本号 1，以及背包、队伍、Field、教程和自动事件快照 |
| InventorySaveData | 有序格子、itemId、数量，以及空条目 |
| PartySaveData | 有序成员、characterId、HP/最大 HP、MP/最大 MP、等级、EXP、isDead |
| FieldSaveData | 场景名、可选的玩家位置与欧拉角旋转、已清除 spawn ID、UTC 记录、已开启 chest ID |
| TutorialSaveData | 已完成教程 ID |
| FieldAutoEventSaveData | 已完成自动事件 ID |

HashSet 和字典会转换为以列表为基础的 DTO 结构，以便 JsonUtility 处理。存档数据不包含 GameObject、控制器引用、Sprite 实例或 ScriptableObject 模板。

有可用的 FieldSaveContext 时，它提供当前 Field 场景与玩家变换。在 Field 之外保存，仍可以记录运行时状态，但不会自动从 Battle 或 Title 获得一份新的 Field 玩家变换。

### 恢复流程

```text
读取 save.json
 -> JsonUtility.FromJson<GameSaveData>
 -> 道具 ID -> ItemDataBase -> 有序运行时格子
 -> 角色 ID -> CharacterDataBase -> 模板副本 + 保存的数值
 -> 恢复 Field 清除 / 开启状态与时间戳
 -> 恢复教程和自动事件完成状态
 -> 存在 FieldSaveContext 时应用玩家变换
 -> 刷新入队点可见性
 -> 调用方刷新 UI 或加载保存的 Field 场景
```

Title Load 恢复状态、清除临时战斗返回数据，并加载存档中的 Field 场景；还保留回退到可用场景的处理。Field 菜单 Load 可以原地应用，也可以切换到存档中的场景。

缺少数据库引用或 ID 时会产生警告，这并不是无害的占位状态。未知道具会留下空格，未知队伍成员可能被跳过，进而导致战斗玩家缺失。

Load 按钮使用 HasSaveFile 禁用无文件时的入口。文件存在，并不能证明 JSON 内容有效，也不能证明里面所有 ID 都能解析。

### 当前持久化边界

| 边界 | 影响 |
| --- | --- |
| Attack、Speed 和最大行动值没有保存，也没有按等级重新计算 | 读取升级角色时，等级、HP/MP 可以恢复，但战斗属性可能使用模板值 |
| Load 不会完整地原地重建 Field | 不会统一校正所有现有宝箱与敌人视觉；入队有专门刷新 |
| 自动结局条件在 Start 中检查 | 把进度加载到当前活动场景时，不会自动重新运行启动检查 |
| 单文件，Save 直接写入 | 没有多存档槽 UI、原子替换、备份恢复或迁移流程 |
| Save 捕获写入失败，Load 尚未完整包装异常处理 | 损坏 JSON 或文件访问错误仍需要加固 |
| 没有战斗中途快照 | 不恢复当前行动者、行动值、增援、技能冷却和动画 |
| 没有表现进度快照 | 不恢复当前镜头角度，也不恢复 VN 对话进行到一半的位置 |
| 使用场景名与全局 ID | 重命名内容时，需要有意识地处理兼容或迁移 |

“已经实现可序列化快照”是准确的；“Save/Load 恢复所有可能的运行时状态”则不准确。

## 20. 当前内容与配置步骤

### 当前 Field 内容

迷宫 FieldData 包含五个永久敌人遭遇点、三个动态生成宝箱，以及一个 Argo 入队条目。宝箱条目共用 prefab，并选择不同的奖励资源。

Boss FieldData 包含固定不动、永久不刷新的 `boss_spawn_001`。它的遭遇会解析 `boss_001`，发放 500 EXP，并配置必定掉落一至两个 Potion。

Boss 角色当前拥有 Fire 与 MagmaBurst，权重分别为 3 和 1。MagmaBurst 是敌方群体攻击，冷却为两个自身回合。测试证明数据链路通了，并不等于统计上验证了配置概率。

### 添加敌人遭遇

1. 在 EnemyCharacterDataBase 中加入具有唯一 ID 的敌方 Character 模板。
2. 配置属性、头像、技能、AI 类型和可选权重。
3. 创建并注册 EncounterData，填写匹配的 EnemyEntries 和奖励。
4. 添加 Field 生成条目，配置唯一 spawnId、encounterId、prefab、变换和刷新策略。
5. 需要墙阻挡时，为追踪和联合检测配置障碍物 mask。
6. 检查查询与来源日志，测试进入战斗、死亡、奖励、返回和持久化。

### 添加宝箱

1. 创建 ChestRewardData，填写需要的道具与数量列表。
2. 复用一个交互与阻挡 Collider 都正确配置的宝箱 prefab。
3. 添加 FieldObjectEntry，配置唯一 objectId 和奖励覆盖资源。
4. 确认生成后的控制器收到了 objectId 和奖励资源。
5. 测试开启、toast、解除碰撞、保存，以及重新进入场景后的恢复。

### 添加入队点

1. 确保 CharacterDataBase 包含可入队角色。
2. 使用逻辑点 prefab，以及只负责表现的视觉 prefab。
3. 配置 Field 入队条目，包括可选的入队前对话。
4. 确保目标 HUD 与战斗 prefab 容量支持加入后的队伍。
5. 测试重复入队防护，以及入队前后的 Load。

### 连接结局

1. 使用 Boss 点实际的 spawnId 作为清除条件。
2. 配置唯一自动事件 ID 和 DialogueData。
3. 绑定选定的普通或 VN 展示器。
4. 把对话完成事件连接到 DemoEndController。
5. 测试胜利、逃跑、重复进入，以及在事件前后读档。

## 21. 环境制作与测试依据

### 制作职责分离

手工制作的环境应放在 Environment 下。数据驱动的遭遇、宝箱与入队点应放在各自的动态生成根节点下。并不需要每次进入区域，都由 FieldData 重新创建完整的 3D 地形。

当前 Boss 场景视觉工作包括复用岩石模型与材质，以及一个大型火山灰盒体块。它是构图占位物，不是完成的火山模型。

使用共用的较低 Field 镜头时，预期的首个画面应让入口方向、Boss 和背景地标都清晰可读。构图与测试应基于 Game 相机，不要只看 Scene 视图。

### 本地火山工具

`ArtSource/VolcanoCave/` 包含生成与布局源文件、报告。Unity 编辑器工具支持布局导出、环境构建和试玩验证。

当前构建器限制在编辑模式下的 FildScene，并在 `Library/VolcanoCave/SceneBackups` 下创建场景备份。它不是用于 Boss_FieldScene 的通用安全重建器。

视觉淡化不会改变实体碰撞。除了画面效果，还应分别检查碰撞、视线 mask、遭遇连线和镜头构图。

本次检查时，有些环境材质、prefab 和编辑器工具还是未追踪的本地文件。它们存在于工作区，并不保证新的克隆仓库也包含它们。决定提交 Unity 资源时，必须同时提交对应的 .meta 文件。

### 手动回归记录

实际测试结果和备注应记录在 `docs/VOLCANO_ROUTE_PLAYTEST.md` 中。历史环境报告可以作为支持依据，但不能当作当前独立构建的性能基准。

建议发布前覆盖：

- 从 New Game 开始，走完迷宫、入队、宝箱、普通与联合遭遇、Boss、结局和返回标题。
- 在移动背包格子、消耗道具、队员死亡、入队和清除生成点后测试 Save/Load。
- 战斗死亡与复活、死亡时间轴图标、结算与 HUD 顺序稳定性，以及增援入场。
- 逃跑返回的遇敌冷却和场景渐变行为。
- 教程跳过确认、隐藏 UI 暂停，以及背包与菜单的 Escape 输入归属。
- Boss 单体与群体技能目标、加权选择、冷却时机和镜头切换。
- VN 左右头像、可选背景与打字效果、跳过与完成，以及事件重复进入或读档行为。
- 新克隆或构建后的资源引用，以及默认玩法 UI 可见性。

## 22. 剩余工作与优先级

### 已有基础

项目已经具备核心 Field/Battle 闭环、表形式的遭遇解析、有序共享背包、恢复与复活道具、定时刷新、考虑墙壁的检测与联合判断、增援、入队点、宝箱奖励覆盖、普通与 VN 对话、带跳过确认的教程、结局事件和 JSON 快照。

这些基础足够支撑一个范围受控的可玩 demo，并不意味着要在完成体验之前继续加入所有 RPG 子系统。

### 已知缺口

| 领域 | 尚未完成的部分 |
| --- | --- |
| 装备 | 装备数据、槽位、属性应用、UI 和持久化 |
| 道具效果 | 完整 buff 道具执行，以及更丰富的效果规则 |
| Boss 行为 | 更多技能与条件、考虑可支付消耗的选择，以及根据执行成功与否登记冷却 |
| 成长持久化 | 保存 Attack/Speed 和其他派生属性，或根据规则确定性地重新计算 |
| Load 可靠性 | 原子或备份存档、带防护的读取与解析、迁移，以及完整的原地对象状态同步 |
| Field 导航 | 寻路，而不是直接移动加卡住恢复 |
| 暂停归属 | 协调模态界面的暂停所属关系，而不是一个共用 bool |
| 教程深度 | 由玩家行动驱动的交互式教程条件 |
| VN 内容制作 | 最终对白、头像与表情、背景，以及更丰富的逐行表现 |
| 环境 | 山洞封闭感、Boss 火山构图，以及碰撞、灯光与美术一致性 |
| 数据流程 | 外部 Excel/CSV 导入导出与内容校验 |
| 测试 | 完成回归记录，以及独立构建和新克隆验证 |

外部表以后可以通过编辑器流程导入：

```text
Excel / CSV 制作数据
 -> 解析并校验 ID 与引用
 -> 生成或更新 ScriptableObject 资源
 -> 现有运行时数据库查询
```

这是扩展方向，并不是项目已经实现的导入器。稳定 ID 和集中的查询边界，就是为它做的准备。

### 建议顺序

1. 完成从山洞到开阔 Boss 区域的构图，以及必要的美术替换。
2. 跑完整 demo，修复具体的回归问题。
3. 使用现有配置路径完成 Boss 与 VN 内容。
4. 加固影响最大的持久化与暂停边界。
5. 在 demo 确实需要时，再加入装备或更广泛的表工具。
