# Adventure of Paul Demo Technical Overview

> Source and configuration snapshot: 2026-10-04.
> Format: English main text with a Chinese summary for each chapter.
> Full Chinese translation: [TECHNICAL_OVERVIEW_ZH.md](TECHNICAL_OVERVIEW_ZH.md).

## 1. Purpose and Current Scope

This document describes the implementation currently present in the workspace, including the Demo v1 gameplay loop and the v2 presentation work added on top of it. It is not a claim that every path has passed a fresh regression test.

The project is a Unity turn-based RPG demo with two Field areas, a shared Battle scene, persistent party/inventory state, and a Boss ending flow:

```text
TitleScene
 -> FildScene: enclosed maze exploration
 -> Chest / recruit / dialogue / regular or group encounter
 -> BattleScene: turn-based combat
 -> Rewards / party writeback / return to Field
 -> Boss_FieldScene: open confrontation area
 -> Boss battle in BattleScene
 -> Return to Boss_FieldScene
 -> Automatic ending dialogue
 -> DemoEndPanel
 -> Back to TitleScene
```

The presentation direction is an oppressive enclosed volcanic cave followed by an open Boss arena with a large volcano behind the Boss. The existing greybox and imported environment are implementation steps toward that direction, not finished art.

### Evidence Levels

| Evidence | What it establishes |
| --- | --- |
| Gameplay source | Implemented responsibilities, branches, and data flow |
| Serialized scene/asset configuration | Inspector bindings and values saved to disk |
| `docs/VOLCANO_ROUTE_PLAYTEST.md` | User-recorded manual checks, notes, and unfinished cases |
| `ArtSource/VolcanoCave/` reports | Historical environment generation and playtest evidence |
| This documentation update | Source/configuration review only; no new Unity Play Mode or build test |

A script existing, a reference being assigned, and a complete end-to-end test passing are different milestones. Keep those distinctions when describing portfolio progress.

**中文概括：** 这份文档对应当前代码和已保存配置，不再只描述早期 v1。v1 的玩法闭环已经具备，v2 正在增加 VN 演出、Boss 技能行为、共享镜头配置和火山环境。文档检查不等于本次重新跑完测试，测试结果仍以测试清单中的实际记录为准。

## 2. Platform, Scenes, and Repository Map

### Technology

| Component | Current use |
| --- | --- |
| Unity | `2022.3.62f2` |
| Rendering | Built-in Render Pipeline; no active custom render pipeline in Graphics Settings |
| Input | Gameplay uses `UnityEngine.Input`; project input handling is set to Both |
| UI | uGUI, CanvasGroup, TextMeshPro |
| Animation/tweening | Bundled DOTween for panel, timeline, and chest presentation |
| Configuration | ScriptableObject assets containing serializable entries |
| Persistence | `JsonUtility` JSON file under `Application.persistentDataPath` |
| Navigation | AI Navigation package exists, but Field enemy movement is not NavMesh-driven |

Enabled Build Settings scenes are `TitleScene`, `FildScene`, `BattleScene`, and `Boss_FieldScene`. The spelling `FildScene` is the actual scene name and must be used by scene-loading code.

### Source Map

| Location | Contents |
| --- | --- |
| `Assets/Scripts/Filed/` | Field creation, movement, cameras, encounters, inventory, interactables, transitions |
| `Assets/Scripts/Battle/` | Battle orchestration, spawning, formation, targets, camera, tutorial entry |
| `Assets/Scripts/Character/Controller/` | Character effect execution and player/enemy controllers |
| `Assets/Scripts/Data/` | Character/item/skill/dialogue/tutorial/chest configuration, runtime state, save DTOs |
| `Assets/Scripts/UI/` | Battle and Field presentation, VN dialogue, tutorials, popups, demo ending |
| `Assets/Scripts/Manager/` | Save system and character result flow |
| `Assets/Scripts/Environment/` | Volcanic environment visibility support |
| `Assets/Scripts/DebugTools/` | Save/load, recruitment, revival, and diagnostic helpers |
| `Assets/Art/Environment/VolcanoCave/` | Unity-side environment models, materials, shaders, prefab |
| `ArtSource/VolcanoCave/` | Generation source, layout, manifest, previews, and historical reports |
| `Assets/Editor/VolcanoCave/` | Local environment export/build/playtest editor tools |
| `docs/` | Technical overview, scene setup guide, design plan, and manual test record |

The existing folder name `Filed` and class names such as `FiledPartyHudItem` are retained here to match the code. This documentation update does not rename them.

**中文概括：** 项目当前使用 Built-in 渲染管线，不应因为导入素材曾提示 URP 依赖就把项目写成 URP。虽然安装了导航包，敌人移动仍是自写的状态机移动。文档中的拼写按真实路径保留，避免照着文档找不到脚本或加载不到场景。

## 3. Data Ownership and Stable IDs

### Five Distinct Layers

| Layer | Examples | Responsibility |
| --- | --- | --- |
| Configuration | `FieldData`, `EncounterData`, `CharacterDataBase`, `EnemyCharacterDataBase`, `ItemDataBase`, `SkillData` | Authored templates and lookup sources |
| Cross-scene runtime state | `PartyRuntimeState`, `InventoryRuntimeState`, `FieldBattleContext`, `TutorialRuntimeState`, `FieldAutoEventRuntimeState` | Current progress and mutable game state |
| Battle copies | `BaseController.data`, created through `Character.Copy()` | Per-battle HP/MP, action values, effects, and turn flags |
| Save DTOs | `GameSaveData`, `PartySaveData`, `InventorySaveData`, `FieldSaveData` | Serializable values and IDs, without scene/component references |
| Presentation | HUDs, timeline icons, popups, dialogue panels | Display state supplied by the other layers |

`Character` is a serializable C# class, not a ScriptableObject asset by itself. Character database assets contain lists of these templates.

Battle spawning, recruitment, and loading use character copies. However, `PartyRuntimeState.InitializeIfEmpty` currently adds initial party members directly from the supplied list. Initial setup is therefore not a universal deep-copy boundary.

### ID Dictionary

| ID | Meaning | Important distinction |
| --- | --- | --- |
| `fieldId` | Identity metadata on a FieldData asset | Scene loading currently uses scene names, not a fieldId registry |
| `spawnId` | One placed/generated enemy encounter point | Used for clear state, respawn timestamps, and ending requirements |
| `encounterId` | One battle team/reward configuration | Several spawn points may reference the same encounter |
| `enemyId` in EnemyEntries | Enemy template lookup key | Matches `Character.characterId` in EnemyCharacterDataBase |
| `characterId` | Party or enemy template identity | Used for party matching, recruitment, and save restoration |
| `itemId` | Item template lookup key | Saved instead of an ItemData reference |
| `objectId` | Generated Field object identity | Passed to FieldChestController as its runtime chest ID |
| `chestId` on FieldChestController | Opened-chest persistence key | Must be stable and unique across Field scenes |
| `chestId` on ChestRewardData | Reward asset metadata | Does not independently override the controller's persistence key |
| `recruitId` | Recruitment point identity/configuration | Joined state is currently derived from party character membership |
| Scene spawn point ID | Named arrival destination, such as `FromMaze` | Not an enemy spawnId and does not select an encounter |
| `tutorialId` / auto event ID | One-time tutorial/event completion key | Stored in their own runtime completion sets |

Persistent spawn/chest/event IDs should be globally unique across areas. Current completion sets are not nested under a scene or fieldId namespace.

### Lookup Databases

- `CharacterDataBase`: party templates, with a compatibility lookup through assigned initial party data.
- `EnemyCharacterDataBase`: enemy templates, including stats, portrait, skills, AI type, and skill weights.
- `ItemDataBase`: itemId to ItemData.
- `EncounterDataBase`: encounterId to EncounterData.
- `EnemyDataBase` / `EnemyFieldData`: older Field lookup path retained for compatibility.

Databases describe what an ID means. They do not store how many items the player currently owns or replace the runtime party when saving.

**中文概括：** 配置资产是“模板”，RuntimeState 是“现在的游戏状态”，DTO 是“写进 JSON 的形状”，UI 是“把状态展示出来”。spawnId 管某一个场上的生成点，encounterId 管碰到后进入哪一队战斗，enemyId 最后查到敌方 Character 模板。宝箱奖励资产的 ID 和实际开箱存档 ID 也不是自动等同的，动态宝箱的存档 ID 来自 FieldObjectEntry.objectId。

## 4. Field Startup, Generation, and Player Placement

Main sources: `FieldCreator.cs`, `FieldData.cs`, `FieldSaveContext.cs`, `EnemySpawnManager.cs`.

### Startup Responsibilities

`FieldCreator.Start` performs the following work:

1. Clear Field pause state.
2. Initialize party and inventory only if their runtime state is not already initialized.
3. Generate enemy spawn markers from FieldData.
4. Generate configured objects and recruitment points.
5. Place the player using the applicable return/arrival/load/default position.
6. Ask EnemySpawnManager to spawn encounter enemies.
7. Refresh the party HUD.
8. Start encounter cooldown when returning from Battle and clear transient return data.

Initial inventory is not re-added on every scene entry. An initialized empty inventory remains empty.

### FieldData Entries

| Entry | Main configuration |
| --- | --- |
| FieldSpawnPointEntry | spawnId, encounterId, Field prefab, position, Euler rotation, wander radius, stationary flag, respawn type/time, legacy enemyId |
| FieldObjectEntry | objectId, prefab, chest reward override, position, Euler rotation, scale |
| FieldRecruitPointEntry | recruitId, characterId, logical point prefab, visual prefab, transform, prompt, optional pre-recruit dialogue, disable-after-recruit |

FieldData is a gameplay-point layout/configuration source. It is not currently a terrain generator, procedural dungeon engine, or complete scene replacement.

### Generated Roots

| FieldCreator field | Intended contents | Fallback if not assigned |
| --- | --- | --- |
| `generatedSpawnPointRoot` | Generated enemy spawn markers | FieldCreator transform |
| `generatedInteractableRoot` | Chest/recruit points | Spawn point root |
| `generatedEnvironmentRoot` | Other generated environment objects | FieldCreator transform |

The current Field scenes assign spawn-point and interactable roots. Large hand-authored environment meshes and walls may remain under `Environment` independently.

Assign explicit roots when the goal is to keep the manager object free of generated children. Actual enemies instantiated by EnemySpawnManager are not automatically parented under the spawn-marker root.

When FieldData provides generated spawn markers, FieldCreator passes them into EnemySpawnManager. Without FieldData, the existing scene-configured spawn point path remains available. Object/recruit entries with an entirely zero scale are corrected to `Vector3.one` with a warning.

### Player Placement Priority

```text
Battle return transform
 -> Pending named Field-to-Field arrival point
 -> Pending saved player transform
 -> FieldCreator.playerStartPoint
```

`FieldPlayerTransformUtility.Teleport` synchronizes Rigidbody position/rotation, clears velocity, updates the Transform, and synchronizes physics. This avoids the Rigidbody restoring an old position immediately after a load/teleport.

**中文概括：** FieldCreator 是启动和生成管理器，FieldData 配置玩法点，不意味着所有场景美术都必须运行时生成。Enemy、宝箱/NPC、普通环境可以分开放在各自 root。玩家落点有明确优先级：战斗返回、跨场景命名落点、读档位置、默认出生点；安全传送同时处理 Rigidbody，避免“闪过去又回原位”。

## 5. Field-to-Field Transitions

Main sources: `FieldSceneTransitionTrigger.cs`, `FieldSceneSpawnPoint.cs`, `FieldSceneTransitionContext.cs`, `SceneTransitionController.cs`.

A scene transition has two separate objects:

- A trigger in the source area specifies the target scene and target spawn point ID.
- A FieldSceneSpawnPoint in the destination area provides the corresponding position/rotation.

```text
Player enters transition trigger
 -> Store target scene + destination spawn point ID
 -> Fade / scene load
 -> Destination FieldCreator consumes the arrival request
 -> Find matching FieldSceneSpawnPoint
 -> Teleport player there
```

For example, `FromMaze` means “the arrival point used when coming from the maze.” It is a label matched by code, not a reserved Unity keyword.

A return trigger in Boss_FieldScene can target a different named point in FildScene. Place the arrival point outside the destination trigger volume to avoid an immediate bounce back.

FieldCreator's default playerStartPoint remains useful for New Game and entries without a named destination; it is not the only available landing point.

Battle transitions also use the scene fade path. Field movement must be stopped during the transition so actors do not keep pushing into each other behind the fade.

**中文概括：** 传送 Trigger 是“出口按钮”，SpawnPoint 是“另一边落在哪里”。FromMaze、MazeEntrance 等名字只是两边约定的字符串。双向传送各自配置目标场景和落点，不会因为 FieldCreator 有默认出生点就一定回到默认位置。

## 6. Field Enemy State Machine, Sight, and Respawn

Main sources: `EnemyFieldController.cs`, `EnemySpawnPoint.cs`, `EnemySpawnManager.cs`, `FieldBattleContext.cs`.

### Movement State Machine

Field enemy behavior now uses `Wander` and `Chase` states in Update, replacing the earlier movement coroutine structure.

| State/condition | Behavior |
| --- | --- |
| Wander | Pick a random point in an XZ circle around the spawn origin, move toward it, then wait |
| Chase | Move toward the player while detection/distance conditions remain valid |
| Field paused | Do not update enemy movement |
| `isStationary` | Skip wandering/chasing movement; retain encounter functionality |

The random sample is a Vector2 because the wandering area is a horizontal disk. Its two components are mapped to world X/Z.

Movement remains direct Transform-based motion. Stuck detection periodically checks progress and repicks a wander target after insufficient movement. It reduces persistent wall-pushing, but does not find a navigable path around walls.

### Line of Sight

The chase check uses distance plus an elevated `Physics.Linecast`. Configure the obstacle LayerMask to include the physical maze walls; trigger colliders are ignored by the sight check.

A zero obstacle mask retains the old distance-only behavior. An installed navigation package does not automatically enable navigation or wall-aware detection.

### Persistent Spawn State

`FieldBattleContext` holds:

- Cleared spawn IDs in a HashSet.
- Cleared UTC timestamps in a dictionary.
- The transient battle return transform and encounter lists.
- A short post-battle encounter cooldown.

HashSet membership answers “has this spawn been cleared?” without adding duplicate IDs. The timestamp dictionary answers “when was it cleared?”

| Respawn type | Behavior after victory |
| --- | --- |
| Permanent | Continue skipping this cleared spawn |
| Timed | Skip until the configured duration has elapsed |
| Timed with non-positive delay | Clear the record and allow spawning |

A missing timestamp for a timed cleared spawn is initialized to the current UTC time. UTC timestamps are written in round-trip string form for the save DTO.

Timed respawn uses real-world UTC time, so elapsed time can include time outside the game. EnemySpawnManager also polls timed points while the Field scene is active, at a configurable interval, rather than requiring a scene reload.

`HasActiveEnemy` prevents another enemy being instantiated for a spawnId that already has a live registered enemy. `RegisterActiveEnemy` maintains that association.

### Post-battle Cooldown Is Different

The short encounter cooldown uses `Time.time`, not UTC. It prevents an immediate new encounter after returning from Battle, including Escape. It is not an invincibility/damage system and does not itself prevent enemies from chasing or physically blocking the player.

**中文概括：** 场上敌人现在是 Wander/Chase 状态机，Boss 的 stationary 可以只禁止移动而保留遇敌。射线负责隔墙索敌，卡住检测负责重新抽游荡点，但两者都不是寻路。小怪刷新时间用 UTC，退出战斗的短暂遇敌保护用游戏时间，是两套不同机制。

## 7. Group Encounters and Battle Reinforcements

Main sources: `EncounterTrigger.cs`, `FieldBattleContext.cs`, `BattleSpawner.cs`, `BattleFormation.cs`.

### Field-side Collection

When the source enemy is chasing, its encounter trigger can collect nearby active enemies within the configured horizontal radius. Optional line-of-sight checks reject allies separated from the source by blocking walls.

Collection is source-centered and one-hop: it does not recursively expand from every newly found ally. Nearby candidates do not all need to be in Chase themselves.

```text
Source enemy touches player
 -> Reject if paused / already triggered / within encounter cooldown
 -> Collect eligible nearby enemy encounter points
 -> Record spawn IDs and encounter IDs in FieldBattleContext
 -> Load BattleScene
```

Spawn IDs are deduplicated. Encounter IDs deliberately preserve repeated entries: two different Field enemies using the same encounter configuration still contribute two teams.

A victory marks all participating spawn IDs cleared, not only the first trigger.

### Visible Group Links

EncounterTrigger owns pooled runtime LineRenderers for nearby group links. Membership checks run at an interval; line endpoint positions are refreshed in LateUpdate to follow moving actors.

These lines are visible in the game. They are distinct from editor-only Gizmos. A supplied material is preferred; a runtime fallback material is created if none is assigned.

### Battle-side Queue

BattleSpawner resolves every contributed encounter and combines the resulting enemy requests. BattleFormation has a limited number of active enemy slots, currently five by default.

Overflow enemies wait in a queue. When a slot is released, the next enemy is instantiated, registered, and added to the timeline. An enemy waiting in the queue is not already an acting timeline participant.

The current default player formation has four slots. Increasing party size requires corresponding spawn/UI/formation capacity work; recruitment does not itself enforce that limit.

**中文概括：** 联合遭遇把“附近符合条件的多个队伍”送到同一场战斗，相同 encounterId 不会被错误合并掉。五个只是同时上场的默认敌人数量，更多敌人会排队补位。游戏中的联线用 LineRenderer，并不是只有编辑器能看见的 Gizmo。

## 8. Encounter Tables and Battle Spawning

Main sources: `EncounterData.cs`, `EncounterDataBase.cs`, `EnemyCharacterDataBase.cs`, `BattleSpawner.cs`.

### Preferred Data Flow

```text
FieldData.spawnPoints
 -> EnemySpawnPoint: spawnId + encounterId + Field prefab
 -> EncounterTrigger
 -> FieldBattleContext.CurrentEncounterIds
 -> EncounterDataBase lookup
 -> EncounterData.EnemyEntries: enemyId + count
 -> EnemyCharacterDataBase lookup
 -> Character.Copy()
 -> Battle enemy prefab + resolved character data
```

The Field prefab represents the encounter in the exploration scene. The enemy templates and counts in EncounterData define what appears inside Battle.

An EncounterEnemyEntry uses `enemyId`, but the database matches it against the enemy Character's `characterId`. These names describe the same lookup key in different contexts, not two unrelated identities.

### Compatibility Paths

| Situation | Current behavior |
| --- | --- |
| Valid EnemyEntries | Resolve templates by ID and repeat each by count |
| No enemies resolved for an encounter, with legacy fallback enabled | Use that encounter's serialized legacy enemy characters |
| No enemies resolved for the overall battle | Use configured initial battle enemies |
| Old Field spawn setup with missing data and a legacy enemyId | Attempt the older EnemyDataBase lookup path |

Fallback is not necessarily a per-invalid-row replacement. A partially valid EncounterData can produce only its resolved entries. Lookup/source logs are therefore important when checking whether the intended table path was used.

Encounter validation reports empty IDs, invalid entries, and missing drop items. It is diagnostic validation, not a transactional rejection of every malformed asset.

### Player Spawning

Battle players come from PartyRuntimeState and are copied for the battle. Serialized player prefab entries supply scene objects; they are not the source of the current party's HP/MP progression.

Launching BattleScene directly requires an initialized runtime party, for example through a suitable debug entry. BattleSpawner does not automatically invent the normal party from its player prefabs.

Portraits come primarily from the resolved Character data. A prefab controller portrait is only a fallback when the character portrait is absent. HUD, result, and timeline setup must use the same resolved source.

**中文概括：** 现在的首选链路已经是 encounterId 查遭遇表，再由 EnemyEntries 的 enemyId 查敌方模板。旧入口仍在，所以必须看来源日志，不能只凭生成数量判断用了哪条路径。Field 上一个模型可以代表一整队敌人；战斗头像与技能也应来自查到的模板。

## 9. Battle Orchestration and Action-value Timeline

Main sources: `BattleManager.cs`, `BattleTargetSelector.cs`, `TimeLineUI.cs`, `TimelineIconView.cs`.

BattleManager remains the central orchestrator. It coordinates spawning, readiness, turn progression, commands, target selection, skill execution, camera movement, death/revival, rewards, and result events.

Important events include `OnTimeLineOrdered`, `OnCurrentActorChanged`, `OnTargetChanged`, `OnInputStateChanged`, `OnBattleEnded`, and inventory count changes.

### Turn Progression

While the battle is ready, not ended, and not executing an action:

```text
ActionValue -= Speed * (10 / 0.75) * Time.deltaTime
```

After a completed action, that actor's action value is reset to its maximum. Decrementing action values is suspended while an action sequence is running.

Timeline ordering uses remaining action value relative to Speed for actors not yet ready. Already-ready values have their own ordering path. Invalid, dead, or off-field actors are excluded from useful turn prediction.

LateUpdate requests ordering updates and only publishes a changed order. UI tweening animates icons toward the resulting slots. The timeline is a prediction of the current scheduler state, not a separate queue with its own turn logic.

### Input and Camera Synchronization

Attack, Skill, Item, and Run are command entries. Enemy targeting supports keyboard cycling and mouse targeting; ally/dead-ally selection has its own paths and should not be assumed identical to enemy targeting.

Confirmation is gated while the battle camera is moving. Coroutine branches waiting for the camera yield control instead of spinning in a tight loop, avoiding an Editor freeze when confirmation is pressed early.

### Death and Revival

- A dead party member remains part of the party and can remain available as a revive target.
- A party member entering a new battle with HP zero is not automatically healed.
- Dead members do not get an active timeline icon during spawn.
- Death removes the active icon; revival enables the controller, rebuilds a missing icon, and requests a timeline refresh.
- Enemy death releases its formation slot and permits a queued reinforcement to enter.

Party/result order follows the runtime party's stable identity/order, rather than the order in which controllers died or were re-added.

**中文概括：** 行动值由 BattleManager 计算，时间轴应反映同一个计算结果，不再用另一套固定刷新预测。死亡角色仍保留队伍身份，复活后恢复控制器和图标，不应该因“删掉再加回来”改变队伍或结算顺序。镜头移动期间确认需要等待并 yield，不能忙循环卡死 Editor。

## 10. Skills, Enemy AI Weights, and Cooldowns

Main sources: `Character.cs`, `SkillData.cs`, `EnemyAiType.cs`, `BaseController.cs`, `BattleManager.cs`.

### Skill Source and Effect Execution

Character data holds the preferred skill list. Controller-level skill configuration remains a compatibility source when the character list is empty.

BaseController separates starting a skill from applying its effect:

- Validate/pay the MP cost once.
- Announce the skill once.
- Apply damage/heal/revive to one target or to a target collection.
- Complete the skill use once.

Group execution does not repeatedly pay MP or repeatedly announce the same skill for every target. Damage currently uses Attack plus skill power; healing uses the configured power. Floating damage/heal/MP text still requires an assigned floating-text prefab.

The enemy `EnemyAll` action path targets the opposing live player group, rather than falling into a self-target default. Player-side group-target skill flows are not fully equivalent to this enemy implementation.

### Normal and Boss AI

`Character.enemyAiType` controls enemy decision policy:

- Normal enemies retain a low-HP healing preference, subject to healing-use and cooldown checks.
- Boss enemies skip that generic low-HP healing priority and use their configured damage-skill candidates.
- Damage candidates must be cooldown-ready before weighted selection.

This AI type is separate from EncounterData's intro camera type and the Field stationary flag.

### Weighted Selection

Each EnemySkillWeightEntry references a SkillData and an integer weight. An unconfigured skill has default weight 1.

For Fire weight 3 and MagmaBurst weight 1:

```text
Total weight = 4
Random integer = 0, 1, 2, or 3

Fire cumulative interval:      [0, 3)
MagmaBurst cumulative interval: [3, 4)
```

Among these two eligible skills, MagmaBurst has a 1/4 probability. This is not a guarantee of one cast every four turns.

Weight zero excludes a candidate when some candidates have positive weights. If all eligible weights are zero, the current code warns and falls back to uniform selection; zero is therefore not an unconditional skill-disable switch.

### Enemy Cooldown Turns

`SkillData.enemyCooldownTurns` is interpreted as the number of subsequent turns of that same enemy during which the skill is unavailable.

Cooldowns are kept per actor and per SkillData in BattleManager. Existing cooldowns advance after that actor's turn, then the newly used skill's cooldown is registered. A cooldown of two blocks the next two own turns.

This is not a seconds timer, a global party cooldown, a save field, or a completed player-skill cooldown system.

Current selection filters by cooldown but does not fully pre-filter MP affordability. Execution can reject a skill later; cooldown registration is not based on a returned “effect succeeded” result. This is a future refinement boundary.

**中文概括：** 技能模板负责技能内容，Character 负责角色会哪些技能、AI 类型和权重，BattleManager 负责选择与冷却。3:1 是在“本回合都可选”的前提下抽签，不是固定轮换；MagmaBurst 冷却时概率池也会变化。冷却按该敌人自己的行动回合数计，不按现实秒数计。

## 11. Field and Battle Camera Presentation

### Shared Field Defaults

Main sources: `FieldCameraSettings.cs`, `FieldCameraController.cs`.

The shared configuration asset is:

`Assets/Scripts/Data/FieldData/CameraData/FieldCameraSettings_Default.asset`

Both current Field scenes reference this asset. FieldCameraController reads it in Awake and applies follow/rotation defaults:

| Setting | Shared asset value |
| --- | --- |
| Distance | 6 |
| Height | 3 |
| Smooth Time | 0.05 |
| Default Pitch | -12.5 |
| Rotate Speed | 3 |
| Min / Max Pitch | -15 / 60 |

Yaw remains scene-specific, allowing each scene's entrance to face its own composition. If no settings asset is assigned, the controller's serialized values remain in use.

The asset overrides the corresponding controller fields at startup. It is not a live settings broadcast, and it does not persist the player's current camera angles across scene changes.

Pitch rotates the follow offset; the camera then looks at an elevated player focus point. Its Inspector pitch is not simply the final Camera Transform Euler angle.

Field rotation input is blocked while FieldPauseState is paused. Follow updates can still run. Do not enable a second camera-follow script on the same camera unless the two drivers are explicitly coordinated.

### Battle Intro and Actions

`EncounterData.introCameraType` chooses Normal or Boss presentation. A group containing a Boss-intro encounter selects Boss presentation.

- Normal: establish a confrontation shot using the next friendly actor and a default live enemy target.
- Boss: play the configured battle-start presentation, then establish the confrontation shot.
- Enemy group skill: show the caster, wait, move toward the player-group shot, then execute the effect.

BattleCameraDirector owns camera movement routines and exposes movement state to the command flow. This separates camera interpolation from turn progression, although BattleManager still coordinates much of the sequence.

### Volcanic Occluder Fading

`VolcanoCaveVisibility` fades configured renderers obstructing the camera-to-player view using a shader `_Fade` property and MaterialPropertyBlock.

It does not disable colliders or make walls transparent to gameplay linecasts. A faded wall can still block movement, enemy sight, and group encounters.

This is a renderer-list/shader-specific solution, not automatic transparency support for every imported material.

**中文概括：** 镜头默认参数现在由一个共享配置资产管理，两个 Field 不必逐项重复填写；yaw 仍可根据场景入口单独设定。玩家旋转后的角度暂时不跨场景保存。Boss 镜头演出、Boss AI 和 Boss 不移动是三个不同配置。墙变透明只影响视觉，不代表物理墙被删掉了。

## 12. Rewards, Results, and Return Flow

Main sources: `EncounterData.cs`, `BattleManager.cs`, `BattleEndPanelController.cs`, `ResultCharacterPanelController.cs`, `RewardPopController.cs`, `LevelUpPopController.cs`.

### Reward Ownership

EncounterData defines reward EXP and independent item-drop rows. EncounterRewardService calculates and grants rewards, returning an EncounterRewardResult containing EXP, granted items, and level-up results.

For group encounters:

- Reward EXP is summed across the resolved encounter list.
- Repeated encounter entries contribute repeatedly.
- Each configured drop row rolls independently.
- Successful items are added to InventoryRuntimeState before the popup appears.

The current drop check rejects a roll when `Random.value > dropChance`. Amounts are guarded to at least one, and the integer maximum is included by using `Random.Range(min, max + 1)`.

Each eligible player controller receives the computed EXP amount; it is not divided among the party. A legacy fallback EXP amount of 120 remains when no EncounterData reward source is available.

### Presentation Sequence

```text
BattleManager finalizes outcome / builds payload
 -> BattleEndPanelController displays Win / Lose / Escape
 -> Closing that panel emits OnClosed(payload)
 -> ResultCharacterPanelController shows character results

Win:
 -> RewardPopController
 -> LevelUpPopController for members that leveled up
 -> Return delay
 -> Load originating Field scene

Escape:
 -> Character result display
 -> Return delay
 -> Load originating Field scene

Lose:
 -> Failure buttons
 -> Retry / Back to Title / optional configured Load
```

The reward popup and level-up sequence are orchestrated by ResultCharacterPanelController after the end panel closes, not directly by BattleManager.

RewardPopController shows EXP and granted item lines. With no item drops, it shows EXP only. It does not currently add an explicit “No items” line. With no positive EXP and no items, the reward popup is skipped.

Results are granted in gameplay state before display. A skipped/missing popup does not by itself cancel the granted reward.

### Writeback and Retry

Win and Escape write current battle party state back to PartyRuntimeState. Lose does not perform that writeback, so Retry can recreate the pre-battle party.

Inventory is shared live state: consumed items are not automatically rolled back on Retry. “Retry restores the party” should not be documented as a full transaction rollback.

Result panels start with HideImmediate to avoid a visible frame when entering a new BattleScene.

**中文概括：** 奖励服务先实际发放奖励，再把结果交给 UI 展示。顺序是战斗结束提示、结算卡片、奖励弹窗、升级弹窗、返回 Field。逃跑不发奖励，失败不把失败后的队伍覆盖回运行时队伍，但已消耗的道具暂时不会因为 Retry 自动退回。

## 13. Inventory Structure, Dragging, and Item Use

Main sources: `InventoryRuntimeState.cs`, `InventorySlot.cs`, `DraggableItem.cs`, `FieldInventoryPanelController.cs`, `FieldInventoryPartyTargetPanelController.cs`, `ItemData.cs`.

### Ordered Runtime Slots

InventoryRuntimeState stores ordered slot records containing ItemData and count.

- Default minimum capacity is 20.
- Empty slots are real entries with no item/count.
- Adding an existing item stacks into its current slot.
- Otherwise, addition uses an empty slot or grows capacity if none is available.
- Consuming the final item leaves an empty slot instead of removing that slot.
- Swapping changes slot contents without shifting later slot indices.

There is no hard capacity limit, maximum stack size, equipment slot model, or external item-table importer at present.

### Dragging Hierarchy

```text
ItemSlot                         fixed grid cell / drop target
 -> ItemRoot                     DraggableItem
     -> ItemIcon
     -> CountText
```

Only ItemRoot moves while dragging. Moving the grid cell itself can disturb layout ordering and result in unexpected slot indices.

DraggableItem obtains the source slot index from FieldInventoryItemView. InventorySlot obtains the destination index, swaps runtime data, and asks the panel to refresh. Icon and count move together because they share the draggable parent.

Empty views hide icon/count without deleting the underlying runtime slot.

### Field Use Flow

```text
Select inventory item
 -> Show description
 -> Use
 -> Open separate party-target panel
 -> Select a member
 -> Validate item/target
 -> Apply effect + consume item if valid
 -> Refresh inventory and party HUD
```

The target panel and the ordinary Field party HUD use the same view component type but different object instances and binding modes. Only the target-selection HUD needs clickable target callbacks.

The Use button can be disabled for invalid targets. Selecting a full-HP/full-MP or otherwise invalid member can show a toast explaining the reason; failed validation does not consume the item.

| ItemType value | Implemented meaning |
| --- | --- |
| None = 0 | No usable effect |
| Heal = 1 | Restore living target HP |
| RestoreMp = 2 | Restore MP |
| Revive = 3 | Revive a dead member |
| Buff = 4 | Reserved interface/type; no complete buff-item implementation |

Explicit enum values preserve existing serialized item types when adding new categories.

Battle item selection also uses the shared runtime inventory. Target/effect validation supports recovery and revival; item consumption is not reseeded at every battle entry.

**中文概括：** 背包现在是有顺序的格子数据，不只是临时生成几个图标。20 是最低容量而非硬上限，空格和拖拽顺序都会保存。DraggableItem 挂 ItemRoot，让 icon/count 一起动但格子不动。Field 的选人面板是普通 HUD 的另一组实例，不能直接共用场景上的同一组 HUD 物体。

## 14. Party State, Growth, Recruitment, and Debug Tools

Main sources: `PartyRuntimeState.cs`, `Character.cs`, `CharacterDataBase.cs`, `FieldRecruitController.cs`, `PartyRecruitDebugTester.cs`.

### Party and Growth

PartyRuntimeState stores the ordered party including dead members. Writeback matches stable character IDs, with a name-based compatibility fallback, and preserves existing party order.

Character growth currently includes:

| Rule | Current implementation |
| --- | --- |
| EXP to next level | Level * 100 |
| Per-level maximum HP / MP | +5 / +5 |
| Per-level Attack / Speed | +10 / +0.5 |
| On level-up | Refill HP and MP |

Growth is therefore not limited to HP and MP. Saving all derived growth correctly is a separate concern covered in the persistence limitations.

### Logical Point and Visual Child

```text
RecruitPoint
 - FieldRecruitController
 - Trigger collider
 -> VisualRoot
     -> Character model / animation presentation
```

The parent owns interaction and data. The child presents the recruitable character. On recruitment, hiding VisualRoot leaves the logical point available for later refresh/load rollback.

FieldCreator can configure a generic recruit-point prefab through FieldRecruitPointEntry. The character template is resolved by characterId, copied, and appended if not already in the party.

Nested recruitment controllers/colliders on an old character prefab are disabled during generated-point setup to avoid duplicate interaction ownership. Prefer presentation-only visual prefabs for new content.

Optional pre-recruit DialogueData plays before the recruit action completes. Recruitment currently uses the ordinary dialogue panel path.

Joined state is derived from whether PartyRuntimeState contains the characterId, not a separate saved recruit boolean. Loading a pre-recruit save refreshes recruitment visibility from the restored party.

The interaction's player test detects the interacting player through tag/component hierarchy. It does not mean that every eligible character template must itself carry SimplePlayerMovement.

### Debug Recruitment

PartyRecruitDebugTester is a shortcut for adding a configured character directly and refreshing the party HUD. It does not need to simulate interacting with or manually hiding a recruit point.

Recruitment does not currently implement removal, party swapping, a capacity policy, or automatic creation of unlimited HUD entries.

**中文概括：** 入队点的父物体负责逻辑，VisualRoot 只放模型表现，入队后关子节点而不删逻辑点。读档是否让 Argo 重新出现，由读档后的队伍里有没有 Argo 判断。Debug 入队工具只是直接加入数据并刷新 HUD。离队、换队和超过默认四人容量的规则还没做。

## 15. Chests, Interaction Prompts, and Toasts

Main sources: `ChestRewardData.cs`, `FieldChestController.cs`, `FieldInteractionPromptController.cs`, `FieldToastController.cs`, `FieldData.cs`.

### Data-driven Chest Rewards

```text
FieldObjectEntry
 - objectId
 - shared chest prefab
 - chestRewardData override
 -> FieldCreator instantiates prefab
 -> FieldChestController.Configure(objectId, rewardData)
 -> E interaction
 -> Grant items to InventoryRuntimeState
 -> Record opened chest ID
 -> Show toast / opening visuals
```

Reward-source priority is entry-supplied reward data, then the prefab's reward data, then the legacy inline reward list. This permits several chests using one prefab to grant different authored rewards.

Chest rewards are currently fixed item/count lists. Encounter drops are probabilistic; chest rewards do not automatically inherit that roll system. Gold is not a completed shared runtime reward/currency system.

### Collision and Visuals

A chest's interaction trigger and physical blocking collider serve different purposes. Before opening, the blocking collider prevents walking through the chest. Opening disables blocking while updating closed/open visuals or animating the assigned lid.

ClosedVisual and OpenedVisual are references to alternate representations, not item reward data. A lid animation is optional and depends on the supplied hierarchy.

Opened IDs persist through FieldBattleContext and its save snapshot. A newly created chest applies its opened state on startup. Same-scene Load does not currently run a dedicated refresh over every existing chest; visual rollback is not guaranteed by the DTO alone.

### Shared UI

- FieldInteractionPromptController shows the E/key prompt for the current owner. Optional prompt text can be omitted for a key-icon-only UI.
- The prompt uses CanvasGroup directly; it is not a BasePanel subclass.
- FieldToastController inherits BasePanel and shows short non-interactive messages.
- Chest, recruit, inventory-use failure, and menu save/load paths call the toast presenter with a message.
- A new toast replaces the current message and resets its duration; there is no message queue.

The message producer knows the gameplay result. ToastController only presents that message.

**中文概括：** 动态宝箱也能用一个 prefab 搭配不同 ChestRewardData，FieldCreator 在生成时把配置传进去。开箱存档记录的是控制器的稳定 ID。开盖、碰撞和发奖励各管一件事；toast 并不会自己查宝箱奖励，而是 Chest/Recruit 等逻辑把结果文字传给它。

## 16. Ordinary Dialogue and VN Presentation

Main sources: `DialogueData.cs`, `DialoguePanelController.cs`, `VNDialoguePanelController.cs`, `FieldDialogueController.cs`.

### Shared Dialogue Data

DialogueData contains:

- A dialogueId and ordered lines.
- One optional background sprite for the dialogue.
- A typewriter toggle and characters-per-second setting.

Each line contains speaker ID/name, portrait sprite, portrait side, and text. Speaker ID is metadata; the current panel does not automatically resolve expressions or portraits through CharacterDataBase.

### Two Presenters

| Presenter | Current behavior |
| --- | --- |
| DialoguePanelController | Existing compact dialogue UI, optional next button, pointer/keyboard advancement |
| VNDialoguePanelController | Background image, left/right portrait slots, speaker/text display, pointer/keyboard advancement without a next button |

VN portrait-side data chooses the corresponding slot. The other displayed portrait can remain visible but dimmed, rather than every speaker being recreated on the left.

When DialogueData has no background sprite, the bound VN background image is hidden. If no Image reference is bound, the script simply cannot modify a background image. A separate DimOverlay remains a UI layer; it is not camera blur.

### Optional Typewriter

Typewriter behavior is configured per DialogueData, not independently per line. It uses visible-character progression:

```text
Advance while text is revealing -> reveal the whole current line
Advance after reveal finishes   -> show the next line
```

Disabling the toggle produces immediate text. Typewriter timing uses unscaled time.

Completion invokes the caller's callback. Escape currently also completes/closes the dialogue, so an automatic event can advance through its completion path when dialogue is skipped.

No branching choices, per-line background switching, expression database, voice playback, or full cutscene timeline is implemented by this basic presenter.

**中文概括：** 普通对话和 VN 使用同一种 DialogueData，区别在展示器。VN 支持左右人物、背景图和按整段配置启用的打字效果，没有 NextButton。背景图为空时隐藏的是绑定的背景 Image，不是整个面板。当前还不是完整视觉小说引擎，分支、配音、逐句表情等需要后续扩展。

## 17. Boss Ending Events and Demo Completion

Main sources: `FieldAutoDialogueEventController.cs`, `FieldAutoEventRuntimeState.cs`, `DemoEndController.cs`, `FieldEndingController.cs`.

### Automatic Post-battle Flow

```text
Boss encounter victory
 -> Mark participating spawnId cleared
 -> Return to Boss_FieldScene
 -> FieldAutoDialogueEventController.Start checks conditions
 -> Play assigned ordinary or VN dialogue
 -> Dialogue completes
 -> Mark event completed
 -> Invoke onDialogueFinished
 -> DemoEndController.ShowDemoEnd
```

The condition is the cleared Field spawnId, not an enemy template ID. It still works when FieldCreator dynamically generated the Boss point, because victory writes the same spawnId into FieldBattleContext.

Current Boss setup uses:

| Configuration | Current value |
| --- | --- |
| Boss spawnId | `boss_spawn_001` |
| Encounter ID | `encounter_boss_001` |
| Enemy template ID | `boss_001` |
| Respawn / movement | Permanent / stationary |
| Auto event ID | `boss_ending_001` |
| Required cleared spawn ID | `boss_spawn_001` |
| Presenter | VN dialogue panel |
| Completion listener | DemoEndController.ShowDemoEnd |

The controller checks at scene startup; it is not continuously monitoring every frame. Selecting the VN path requires the VN panel reference. A missing requested presenter warns rather than silently switching to the ordinary panel.

FieldAutoEventRuntimeState stores completed IDs in a HashSet and serializes them as a list. The associated FieldAutoEventSaveData class currently resides in the same source file.

### Ending Panel

DemoEndController hides its panel initially, pauses Field when showing the ending, and provides return-to-title behavior. It restores pause ownership after dialogue completion because dialogue closes its own pause first.

BasePanel is currently a concrete MonoBehaviour class, not abstract. It can be used as the simple end-panel presenter in this implementation.

FieldEndingController is a separate optional interaction-trigger ending path. It is not required before the Boss automatic dialogue flow can work.

**中文概括：** Boss 打完后自动播放剧情依靠“返回 Boss 场景时检查 boss_spawn_001 是否已清除”，不必走进 EndingTrigger。动态生成不会妨碍检查，因为保存和检测用的是同一个 spawnId。剧情结束事件接 DemoEndController，已完成事件 ID 也会进入存档。

## 18. Tutorials, Pause State, and Input Ownership

Main sources: `TutorialData.cs`, `FieldTutorialController.cs`, `BattleTutorialController.cs`, `TutorialPanelController.cs`, `TutorialRuntimeState.cs`, `FieldPauseState.cs`.

TutorialData currently describes ordered pages of title/message text. It is a basic instruction sequence, not an action-conditioned tutorial that waits for the player to perform Attack or select a particular target.

Field and Battle entry controllers start an uncompleted tutorial after their configured delay. Completed IDs suppress replay. The Field entry also avoids its normal startup tutorial path during a battle return/cooldown.

Panel input supports pointer/keyboard advancement and a skip confirmation UI. Confirmed skipping uses the completion callback too, marking the tutorial completed. A missing confirmation root has a warning/fallback path.

Explicit input guards prevent:

- Escape intended for a tutorial from also opening the Field escape menu.
- A battle command button from executing while the tutorial is active.
- The same Escape press that closes the inventory from opening the escape menu.

### Pause Is a Cooperative Flag

FieldPauseState currently wraps a single bool. It is not a pause-token stack and does not set Time.timeScale.

Consumers must explicitly consult it. Field movement, enemy movement, and camera rotation have pause handling, but this does not automatically suspend every system:

- Field camera follow may continue.
- Timed spawn polling has no general FieldPauseState gate.
- UTC-based respawn time continues to elapse.
- BattleManager's action-value Update is not globally frozen by FieldPauseState alone.

CanvasGroup hidden/interactable/raycast settings also matter. An invisible tutorial whose controller still owns pause can appear to be a gameplay freeze.

Overlapping modals need care: one controller clearing a shared bool can unintentionally unpause another modal. A token/reference-count pause service is a possible future improvement, not part of the current implementation.

**中文概括：** 教程现在是可跳过的说明页，完成或确认跳过后记住 tutorialId。FieldPauseState 只是合作式布尔标记，不会自动暂停所有 Unity 模块。教程、背包、ESC 菜单的按键冲突靠各自入口防护，隐藏 UI 但没结束教程仍可能留下 pause。

## 19. Save/Load Snapshots and Restoration

Main sources: `SaveSystem.cs`, `GameSaveData.cs`, `InventorySaveData.cs`, `PartyMemberSaveData.cs`, `FieldSaveData.cs`, `TutorialSaveData.cs`.

### Save Contents

SaveSystem builds a snapshot and writes:

`Path.Combine(Application.persistentDataPath, "save.json")`

| Snapshot | Stored values |
| --- | --- |
| GameSaveData | Version 1 plus inventory, party, Field, tutorial, auto-event snapshots |
| InventorySaveData | Ordered slots, itemId, count, and empty entries |
| PartySaveData | Ordered members, characterId, HP/max HP, MP/max MP, level, EXP, isDead |
| FieldSaveData | Scene name, optional player position/Euler rotation, cleared spawn IDs, UTC records, opened chest IDs |
| TutorialSaveData | Completed tutorial IDs |
| FieldAutoEventSaveData | Completed automatic event IDs |

HashSets and dictionaries are converted to list-based DTO structures for JsonUtility. Save data does not contain GameObjects, controller references, Sprite instances, or ScriptableObject templates.

FieldSaveContext supplies the current Field scene/player transform when available. Saving outside a Field scene can still capture runtime state, but does not automatically obtain a new Field player transform from Battle/Title.

### Restore Pipeline

```text
Read save.json
 -> JsonUtility.FromJson<GameSaveData>
 -> Item IDs -> ItemDataBase -> ordered runtime slots
 -> Character IDs -> CharacterDataBase -> template copies + saved values
 -> Restore cleared/opened Field state and timestamps
 -> Restore tutorial and auto-event completion
 -> Apply player transform if a FieldSaveContext is present
 -> Refresh recruitment visibility
 -> Caller refreshes UI or loads the saved Field scene
```

Title Load restores state, clears transient battle-return data, and loads the saved Field scene with an available-scene fallback. Field menu Load can apply in place or switch to the saved scene.

Missing database references/IDs produce warnings; they are not harmless placeholders. Unknown items leave empty slots, and unknown party members can be skipped, resulting in missing battle players.

Load buttons use HasSaveFile to disable the no-file path. File existence is not proof that JSON content is valid or all referenced IDs can be resolved.

### Current Persistence Boundaries

| Boundary | Consequence |
| --- | --- |
| Attack, Speed, and maximum action value are not stored/recomputed from level | Loading a leveled character can restore level/HP/MP but use template combat stats |
| No complete in-place Field rebuild on Load | Existing chest/enemy visuals are not universally reconciled; recruitment has a dedicated refresh |
| Auto ending checks run at Start | Loading progress into the same active scene does not automatically rerun that startup check |
| Single file; Save uses direct write | No multi-slot UI, atomic replacement, backup recovery, or migration pipeline |
| Save catches write failures; Load is not fully exception-wrapped | Corrupt JSON/file access errors still require hardening |
| No mid-battle snapshot | Current actor, action values, reinforcements, skill cooldowns, and animations are not resumed |
| No presentation-progress snapshot | Current camera angles and the middle of a VN dialogue are not restored |
| Scene names and global IDs | Renaming content requires an intentional compatibility/migration decision |

“Serializable snapshots are implemented” is accurate. “Save/Load restores every possible runtime state” is not.

**中文概括：** Save 是把实时状态转成 DTO 再写 JSON；Load 是用 ID 查模板，再用保存的数值重建 RuntimeState。存档已经覆盖队伍、背包、位置、清怪、宝箱、教程和自动剧情，但成长后的 Attack/Speed 还没完整保存或重算，同场景读档也不是自动重建全部对象。损坏文件、备份与迁移仍是缺口。

## 20. Current Content and Configuration Recipes

### Current Field Content

The maze FieldData contains five permanent enemy encounter points, three generated chests, and an Argo recruitment entry. Chest entries reuse a prefab while selecting separate reward assets.

Boss FieldData contains a stationary permanent `boss_spawn_001`. Its encounter resolves `boss_001`, grants 500 EXP, and includes a guaranteed configured Potion drop of one to two.

The Boss character currently has Fire and MagmaBurst with weights 3 and 1. MagmaBurst is an enemy group attack with a two-own-turn cooldown. A test showing the data path works is not a statistical proof of the configured probability.

### Add an Enemy Encounter

1. Add a unique enemy Character template to EnemyCharacterDataBase.
2. Configure stats, portrait, skills, AI type, and optional weights.
3. Create/register EncounterData with matching EnemyEntries and rewards.
4. Add a Field spawn entry with unique spawnId, encounterId, prefab, transform, and respawn policy.
5. Configure obstacle masks for chase/group checks when walls should block them.
6. Check lookup/source logs and test battle entry, death, rewards, return, and persistence.

### Add a Chest

1. Create ChestRewardData with the desired item/count list.
2. Reuse a correctly configured chest prefab with interaction and blocking colliders.
3. Add a FieldObjectEntry with a unique objectId and its reward override.
4. Confirm the generated controller receives the objectId and reward asset.
5. Test opening, toast, collision removal, save, and scene-reentry restoration.

### Add a Recruit Point

1. Ensure CharacterDataBase includes the recruitable character.
2. Use a logical point prefab and a presentation-only visual prefab.
3. Configure the Field recruit entry, including optional pre-recruit dialogue.
4. Ensure the target HUD/battle prefab capacity supports the resulting party.
5. Test duplicate prevention and Load before/after recruitment.

### Connect an Ending

1. Use the Boss point's actual spawnId as the cleared condition.
2. Assign a unique auto-event ID and DialogueData.
3. Bind the chosen ordinary/VN presenter.
4. Connect dialogue completion to DemoEndController.
5. Test victory, Escape, repeated entry, and loading saves around the event.

**中文概括：** 以后增加内容主要是在数据库和 FieldData 里配，不需要每次复制整套逻辑。配置链路要完整：角色模板、遭遇表、场上生成点三者各负责一层。教程/UI 绑定正确不代表可以省略实测，尤其要测读档前后和事件边界。

## 21. Environment Authoring and Test Evidence

### Authoring Split

Hand-authored environment belongs under Environment. Data-driven encounter/chest/recruit points belong under their generated roots. A full 3D terrain does not need to be recreated from FieldData on every entry.

Current Boss visual work includes reused rock meshes/materials and a large volcano greybox mass. That is a composition placeholder, not a finished volcano model.

The intended first view should make the entrance direction, Boss, and background landmark readable with the shared lower Field camera framing. Compose/test from the Game camera rather than only from the Scene view.

### Local Volcano Tools

`ArtSource/VolcanoCave/` contains the generation/layout source and reports. Unity editor helpers support layout export, environment build, and playtest validation.

The current builder is scoped to FildScene in edit mode and creates scene backups under `Library/VolcanoCave/SceneBackups`. It is not a general-purpose safe rebuilder for Boss_FieldScene.

Visibility fading preserves physical collision. Check collision, sight masks, encounter links, and camera framing separately from visual appearance.

At the time of this review, some environment materials/prefab files and editor helpers were untracked local files. Their presence in this workspace does not guarantee that a fresh clone contains them. Unity assets must be committed with their corresponding .meta files when intentionally included.

### Manual Regression Record

Use `docs/VOLCANO_ROUTE_PLAYTEST.md` for actual test results and notes. Historical environment reports are supporting evidence, not a current standalone-build performance benchmark.

Recommended release coverage:

- New Game through maze, recruit, chest, normal/group encounter, Boss, ending, and Title return.
- Save/Load with moved inventory slots, consumed items, dead party members, recruits, and cleared spawns.
- Battle death/revival, dead timeline icons, stable result/HUD order, and reinforcement arrival.
- Escape return cooldown and scene fade behavior.
- Tutorial skip confirmation, hidden UI pause, inventory/menu Escape ownership.
- Boss single/group skill targets, weighted selection, cooldown timing, and camera transitions.
- VN left/right portraits, optional background/typewriter, skip/completion, and repeat/load event behavior.
- Fresh clone/build asset references and default gameplay UI visibility.

**中文概括：** 美术环境和玩法生成点应分开管理，Boss 背景灰盒先验证构图，不急着做完整建模。火山工具有特定场景范围，不能直接当成通用场景重建器。测试清单保留真实结果；本地未追踪的美术和工具不等于已在 GitHub 上，资源与 .meta 要成对处理。

## 22. Remaining Work and Priorities

### Implemented Foundations

The project already has the core Field/Battle loop, table-style encounter resolution, ordered shared inventory, recovery/revival items, timed respawn, wall-aware detection/group checks, reinforcements, recruit points, chest reward overrides, dialogue/VN presentation, skip-confirmation tutorials, ending events, and JSON snapshots.

These foundations are enough to support a scoped playable demo. They are not a reason to keep adding every RPG subsystem before finishing the experience.

### Known Gaps

| Area | Not yet complete |
| --- | --- |
| Equipment | Equipment data, slots, stat application, UI, and persistence |
| Item effects | Complete buff-item execution and richer effect rules |
| Boss behavior | More authored skills/conditions, affordability-aware selection, success-aware cooldown registration |
| Growth persistence | Save or deterministically recompute Attack/Speed and other derived stats |
| Load robustness | Atomic/backup save, guarded read/parse, migration, full in-place object reconciliation |
| Field navigation | Pathfinding rather than direct movement plus stuck recovery |
| Pause ownership | Coordinated modal ownership rather than one shared bool |
| Tutorial depth | Action-driven interactive tutorial conditions |
| VN production | Final dialogue, portraits/expressions, backgrounds, richer per-line presentation |
| Environment | Cave enclosure, Boss volcano composition, collision/lighting/art consistency |
| Data pipeline | External Excel/CSV import/export and content validation |
| Testing | Completed regression record and standalone/fresh-clone verification |

An external table can later be imported by an editor pipeline:

```text
Excel / CSV authoring
 -> Parse and validate IDs/references
 -> Generate or update ScriptableObject assets
 -> Existing runtime database lookup
```

That is an extension route, not an importer already implemented in this project. Stable IDs and centralized lookup boundaries are the preparation for it.

### Suggested Order

1. Finish the cave-to-open-Boss-area composition and essential art replacement.
2. Run the end-to-end demo and close concrete regression failures.
3. Complete Boss/VN content using the existing configuration paths.
4. Harden the highest-impact persistence and pause boundaries.
5. Add equipment or broader table tooling when the demo actually needs them.

**中文概括：** 核心系统已经足够承载 demo，当前最重要的是环境、内容和闭环测试，而不是为了每天提交继续扩系统。装备仍在后续计划，外部 Excel 也只是预留了合适的接入边界，并没有现成导入器。先让山洞到 Boss 决战的体验完整，再处理存档/暂停等可靠性缺口。
