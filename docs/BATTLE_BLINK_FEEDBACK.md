# Blink Afterimage / 瞬移残影

## English

### Scope

This is the visual follow-up to the blink-melee checkpoint `3bdf0c17`.
Only `Argo_Lycaon.prefab` opts into `BattleBlinkFeedback`. Departure and arrival
leave a short, horizontally streaked pose echo. This is not a portal, explosion,
or a change to Attack damage, hit timing, camera choreography or blink duration.
Paul, enemies, Field characters, Run/Attack clips and scene lighting are unchanged.
No downloaded VFX package, Shader Graph dependency or project rendering change is
required. The effect uses a native Built-in shader and material.

### Rendering

`BattleMeleeFeedback` emits a departure snapshot before hiding its renderers,
then an arrival snapshot after reaching contact/home and restoring visibility.
An already-close target skips teleportation and its effect as before.

`BattleBlinkFeedback` bakes enabled skinned renderers once per emission, keeping
their current bone pose and texture references. Static mesh parts can share their
original mesh. Scale-aware baking keeps vertices compatible with the source
renderer's world matrix, including Lycaon's large FBX import scale. A pose-bounds
check guards against both giant and tiny afterimages. It does not assign temporary
materials to the real model
or change source meshes, bones, renderer enabled flags or collider positions.

Three offset draws share each baked mesh to soften the silhouette. The shader
splits it into horizontal bands, offsets and dissolves them, and reverses the
scatter direction on arrival. Thin, camera-aligned streak quads accompany the
pose echo. This is a short layered afterimage, not a full-screen blur effect.
Snapshots stay at their emission position instead of travelling across the stage.
They receive/cast no shadows and have no colliders. Source textures are sampled
through property blocks; the model's original material assets are not modified.

### Configuration and Lifetime

On Argo's `BattleBlinkFeedback`:

| Setting | Default | Meaning |
| --- | --- | --- |
| Effect Material | BattleBlink | Shared native afterimage material |
| Duration | 0.20 s | Lifetime of each departure/arrival echo |
| Horizontal Stretch | 0.65 | Horizontal scatter relative to model height |
| Streak Count | 18 | Thin horizontal streaks per echo |

Tint is editable on `Assets/Materials/BattleBlink.mat`. The default is a restrained
pale blue-white. It does not emit a real light or modify environment lighting.
The existing `blinkTime` stays at 0.06 s, and the hit callback is unchanged.

Snapshot age uses scaled time, so pausing freezes the effect without hiding it.
At most four snapshots are retained. Completion releases generated meshes; rapid
emissions retire the oldest snapshot. Cancelling/disabling melee, actor death or
deactivation, disabling the effect, and scene teardown clear temporary resources.
Normal completion lets the final short arrival echo fade naturally. Procedural
streak placement does not consume Unity's gameplay random-number state.
The visual component/material is optional; blink melee still works without it.

### Verification

On 2026-11-14, 90 focused Unity Play Mode assertions passed, including shader
support, visible afterimage pixels without the source model, scale/position bounds,
1280x720 and 800x600 captures, source material/mesh preservation, no gameplay RNG
consumption, pause, bounded snapshots, generated-mesh cleanup, and the existing
blink-melee damage, target-loss, cancellation, return and final-victory checks.
No Unity exceptions, errors or tween warnings were captured in the final run.
The forced-unload fixture waits for result-panel fades first; it does not cover
immediate result-UI teardown. Tests use an isolated save directory.
Checks and captures are stored under ignored `Library/BlinkVfxChecks`; its one-off
script is not a shipped game asset.
This does not claim a standalone build or full-demo playthrough.

## 中文翻译

### 范围

这是瞬移近战提交 `3bdf0c17` 的后续视觉表现。只有 `Argo_Lycaon.prefab` 启用
`BattleBlinkFeedback`：离开和出现时留下短暂的横向线条姿态残影。它不是传送门或
爆炸，也不改变 Attack 伤害、命中时机、镜头编排或瞬移时长。Paul、敌人、探索角色、
Run/Attack 动画及场景灯光保持不变。不需要导入下载的特效包、安装 Shader Graph 或
更改项目渲染设置，而是使用 Built-in 管线原生 Shader 和材质。

### 渲染

`BattleMeleeFeedback` 在隐藏 Renderer 前生成离开残影，到达停靠点或原位、恢复
可见性后生成出现残影。目标已经足够近时，仍跳过瞬移及其特效。

`BattleBlinkFeedback` 每次生成时，将启用的蒙皮 Renderer 烘焙一次，保留当下的骨骼
姿态与贴图引用；静态网格部件可以共用原网格。启用缩放处理的烘焙使顶点与源 Renderer
的世界矩阵匹配，包括莱卡恩较大的 FBX 导入缩放。姿态包围盒检查会同时防止残影过大
或过小。它不会给原模型临时更换材质，也不修改源网格、骨骼、Renderer 启用状态或
碰撞体位置。

同一份烘焙网格偏移绘制三次，以柔化轮廓。Shader 将轮廓分为横向细带，进行偏移与
消散；出现时反向收拢，配合面向相机的横向细线。这是短暂的分层残影，不是全屏模糊。
残影停留在生成的位置，不会沿整个战场移动；它不接收或投射阴影，也没有碰撞体。
通过属性块读取源贴图，不修改角色的原材质资源。

### 配置与生命周期

Argo 的 `BattleBlinkFeedback` 配置：

| 设置 | 默认值 | 含义 |
| --- | --- | --- |
| Effect Material | BattleBlink | 共用的原生残影材质 |
| Duration | 0.20 秒 | 每次离开或出现残影的寿命 |
| Horizontal Stretch | 0.65 | 相对于模型高度的横向拉散幅度 |
| Streak Count | 18 | 每次残影的横向细线数量 |

颜色可在 `Assets/Materials/BattleBlink.mat` 中调整，默认为适量的浅蓝白色。不产生
真实灯光，也不修改环境光。原来的 `blinkTime` 仍为 0.06 秒，命中回调保持不变。

残影年龄使用受时间缩放影响的计时，暂停时冻结但不消失。最多保留四份快照，结束后
释放生成的网格，连续触发时提前回收最旧的一份。取消或禁用近战、角色死亡或停用、
禁用特效以及场景卸载时会清理临时资源。正常完成攻击时，允许最后的出现残影自然
淡出。程序生成的横线位置不消耗 Unity 的游戏随机数状态。视觉组件和材质都是可选
的，缺少时仍可正常使用瞬移近战。

### 验证

2026-11-14 有 90 项 Unity Play Mode 针对性断言通过，覆盖 Shader 支持、隐藏原模型后
仍能看到残影像素、尺寸与位置包围盒、1280x720 和 800x600 截图、保留源材质与网格、
不消耗游戏随机数、暂停、快照数量限制、生成网格清理，以及原瞬移近战的伤害、目标
丢失、中断、归位和最后胜利结算检查。最终运行没有捕获到 Unity 异常、错误或补间
警告。强制卸载测试会先等待结算面板淡入淡出结束，不覆盖结算 UI 立即销毁的情形。
测试使用隔离的存档目录。
检查与截图保存在忽略的 `Library/BlinkVfxChecks` 中；其中的一次性
脚本不作为游戏资源上传。本次不声称完成了独立构建或整个 Demo 的完整游玩测试。
