# Enemy Death Feedback / 敌人死亡反馈

## English

### Scope and Timing

The battle Enemy prefab, including bosses spawned from it, now keeps the model
visible for the lethal recoil before it disappears into a small pale particle
burst. The model stays at its original scale while its material alpha fades to
zero. Player knockdown and revival are unchanged. No sound or white flash is
added, and shared character material assets are not modified.

Default timing is a 0.25-second hit hold, followed by a 0.35-second alpha fade.
Particles fade out over at most 0.7 seconds after emission. The whole
presentation lasts about 0.95 seconds and follows scaled game time.

### Responsibilities

- `BattleDeathFeedback` owns only the visual sequence. It is configured on the
  Enemy prefab with `VisualRoot`, `BattleHitFeedback`, and a dedicated particle
  material referencing the existing soft particle texture. `EnemyDeathFade` is
  also referenced by the prefab to include the Standard Fade shader variant.
- The logical root, collider positions, floating-text anchor, and camera anchor
  do not animate. Existing hit recoil finishes before the alpha fade begins.
- MeshRenderer and SkinnedMeshRenderer material slots receive temporary material
  instances in Standard Fade mode. Original colors and other material properties
  are preserved; only alpha decreases. Shadow casting is disabled while fading.
  Original material references and shadow settings are restored during cleanup,
  and temporary instances are destroyed, including interrupted playback.
- A local, non-looping particle system emits 48 particles, capped at 128. Its
  world-space simulation is independent of the model. Temporary objects are
  cleaned up after playback or when the component is disabled.
- `BattleManager` immediately marks the actor dead, disables its controller and
  colliders, removes enemy target/timeline entries, and then waits for the optional
  visual sequence. Other living actors are not given an additional action lock.
- The dead enemy retains its formation slot until disappearance completes. Only
  then is the slot released and reserve spawning attempted, preventing overlap.
- Pending enemy deaths defer victory until cleanup and reserve spawning complete.
  The final cleanup rechecks the result, including deaths caused by turn-end DOT.
  Multiple simultaneous deaths are tracked independently; duplicate notifications
  for a pending enemy are ignored.
- A missing or disabled feedback component uses immediate cleanup. A missing
  particle material skips the burst but still completes the visual sequence.

### Limits

This is an enemy-only placeholder-model presentation, not a reusable pooling or
revival lifecycle. Future models should remain under `VisualRoot`, with camera
anchors and gameplay colliders outside it. Change the component's timing, color,
and particle count to tune the effect. Standalone-build acceptance and final
art-direction approval are separate from editor verification.
The current alpha fade targets built-in Standard materials. Custom or other
render-pipeline shaders need their own transparent-mode integration; merely
changing their color alpha is not sufficient.

### Verification

48 Unity Play Mode assertions passed on 2026-10-31 using real battle prefabs,
covering resource references, nonlethal/lethal recoil, pause, DOT, five simultaneous
deaths, reserve spawning, delayed final victory, player revival, missing/disabled
feedback, external destruction, particle cleanup, fixed scale, progressive alpha,
transparent blending, source-material isolation, and temporary-material cleanup. Actual rendered captures
were inspected. Temporary checks and images are archived under ignored
`Library/DeathFeedbackChecks`; no real save or scene was modified.
The existing 57-assertion boss rotation regression passed earlier for the initial
death-flow integration. It was not rerun for this alpha-only revision; battle
logic, skill costs, damage, warnings, and the phase-two cycle were not edited.

## 中文

### 范围与时长

战斗 Enemy Prefab 及复用它的 Boss，现在会先保留模型播完致命受击，再用一小团浅色
粒子表现消失。模型保持原大小，只让材质 Alpha 逐渐降到 0。玩家倒下和复活保持原样，
不加入音效或闪白，也不修改角色共享材质资源。

默认先停留 0.25 秒，再用 0.35 秒淡出模型。粒子从发射起最多持续 0.7 秒并逐渐淡出。
整段表现约 0.95 秒，遵循游戏时间缩放。

### 职责划分

- `BattleDeathFeedback` 仅负责视觉顺序，配置在 Enemy Prefab 上，引用 `VisualRoot`、
  `BattleHitFeedback`，以及复用现有柔边粒子贴图的独立粒子材质。Prefab 另引用
  `EnemyDeathFade`，让构建包含 Standard Fade 的 Shader 变体。
- 逻辑根节点、碰撞体位置、飘字锚点和镜头锚点不播放动画；已有受击晃动结束后才开始淡出。
- MeshRenderer 和 SkinnedMeshRenderer 的材质槽使用临时 Standard Fade 材质实例，
  保留原颜色与其他材质属性，只逐渐降低 Alpha；淡出期间关闭投射阴影。
  清理时恢复原材质引用和阴影设置，并销毁临时实例，中途取消也会清理。
- 临时粒子系统单次发射 48 个粒子，上限 128，不循环。粒子使用世界空间模拟，
  独立于模型显示；播完或组件关闭时清理临时物体。
- `BattleManager` 立即标记死亡、禁用角色控制器及碰撞体，并移除敌人的目标和时间轴记录，
  然后等待可选的视觉顺序。不额外锁住其他存活角色的行动。
- 死亡敌人保留站位直到消失完成，随后才释放站位并尝试补位，避免新旧模型重叠。
- 尚未清理完的敌人会推迟胜利判定；消散和补位结束后重新检查战斗结果，
  包括回合末持续伤害致死。多人同时死亡分别跟踪；同一敌人仍在消散时忽略重复死亡通知。
- 没有或禁用了反馈组件时立即清理；缺少粒子材质时跳过粒子，但模型淡出仍能正常结束。

### 限制

这是敌人占位模型的初版表现，不是对象池或敌人复活生命周期。以后替换模型时仍放在
`VisualRoot` 下，镜头锚点与玩法碰撞体留在外面。时长、颜色和粒子数可以在组件中调整。
独立构建验收及最终美术效果确认与编辑器验证分别进行。
当前 Alpha 淡出面向内置 Standard 材质；自定义或其他渲染管线的 Shader，需要另行
接入透明模式，不能只修改颜色的 Alpha。

### 验证

2026-10-31 使用真实战斗 Prefab 的 48 项 Unity Play Mode 断言通过，覆盖资源引用、
非致命/致命受击、暂停、持续伤害、五人同时死亡、后备补位、推迟最终胜利、玩家复活、
缺少/禁用反馈组件、外部销毁、粒子清理、固定模型大小、Alpha 逐渐变化、透明混合、
共享材质隔离及临时材质清理。已检查实际渲染截图。
临时检查与图片归档到忽略的 `Library/DeathFeedbackChecks`，没有修改真实存档或场景。
57 项 Boss 轮转回归在同日最初的死亡流程接入后通过；本次仅修改透明表现，没有重跑
该组检查，也没有修改战斗逻辑、技能费用、伤害、危险预警或二阶段出招顺序。
