# Fire / FireBall Projectile Feedback / Fire 与 FireBall 火球反馈

## English

### Scope

The existing enemy `Enemy/Fire.asset` and player `FireBall.asset` launch an orange fireball with a
yellow core and a short fading trail. On contact, it emits a small spark burst
and calls the original `UseSkill` once. Existing damage text, hit recoil, death
handling, and HP events remain downstream of that call.

The opt-in is an asset reference on `BattleFireFeedback`: the Enemy prefab references
Fire, while the Player prefab references FireBall. Both use the same unmodified
component, material, texture, and particle settings. A skill with the same name
does not match. Burn, MagmaBurst, healing, items, skill data, and boss AI are unchanged. This does not
add audio, model flashing, dynamic lights, or a general skill-effect framework.

### Configuration and Timing

`BattleFireFeedback` holds the controller, that prefab's Fire or FireBall asset, and
`BattleFireParticles.mat`. The material uses the same supported built-in particle
shader and existing soft texture as the death particles, without editing those
assets or allocating material copies.

Defaults are 0.5 seconds of flight, 0.3 seconds of impact/particle recovery, and a
0.65-world-unit projectile diameter. The shot starts near the caster and travels
along a shallow arc toward the selected target's upper collider bounds. This
keeps sparks visible instead of burying them at the center of the cube. Without
a collider, a small height offset is used. No collision or raycast decides damage;
the projectile follows the originally selected character reference.

`BattleManager.PlayEnemyActionSequence` (Fire) and `PlaySkillSequence` (FireBall)
wait for the existing interaction camera to arrive before launching. The arrival callback executes the original
skill and battle-end check, retaining the current damage formula and MP path.
For the current boss, Fire remains 70 Attack + 20 power = 90 damage and 0 MP.
Player FireBall retains Attack + 20 damage and a single 10 MP cost, paid at impact.
The action waits for the impact tail; the camera unlocks in `finally`, including
early exits. Other skills retain their original paths and camera selection.

### Cancellation and Resources

Flight, particles, and impact waits use scaled game time and pause together.
Dead, inactive, or destroyed casters/targets cancel pending impact; a replacement
enemy or revived model is not substituted. The manager also rechecks battle and
MP state before applying the skill. Target death caused by the hit does not cut
off the impact tail. Duplicate playback is rejected.

Missing particle material falls back to the original skill callback; missing or
disabled feedback uses the existing skill presentation. Disabling feedback
during flight cancels that pending impact. Every particle system is collider-free,
uses the shared material, and has a 64-particle limit. Completion, cancellation,
component disable/destruction, and scene unload remove the owned runtime effect.
No actor transform, source material, scene, or real save is edited by the effect.

### Enemy Fire Verification (2026-11-03)

On 2026-11-03, 32 focused Unity Play Mode assertions passed after the upper-body
aim adjustment. Checks used the real Enemy prefab, Fire resource, material, and
camera, with native timeline LateUpdate enabled. They covered delayed/exactly-once
damage and cost, original 90 damage/0 MP, pause/resume, camera framing/unlock,
live particles, repeat rejection, visual fallback, mid-flight disable, destroyed
or dead targets, caster death, lethal impact tails, and runtime cleanup. A paid
runtime-only skill copy verified cost timing without modifying the actual Fire.
Burn and ordinary Attack retained their existing behavior. No Unity errors or
destroyed-target tween warnings were reported in this run.

The existing 57 boss-rotation and 20 timeline-lifecycle assertions were also rerun
and passed, totaling 109 checks for this change. Flight and impact captures from
the actual battle camera were inspected at 1280x720. Temporary helpers, results,
and images are archived in ignored Library folders, not shipped under Assets.
Source scenes and Fire/MagmaBurst asset hashes remained unchanged. A standalone
player build and user approval of the visual timing are still outstanding.

### Player FireBall Follow-up (2026-11-04)

28 new Play Mode assertions passed with the real Player prefab, FireBall asset,
skill-confirmation coroutine, and battle camera. They covered shared-resource
configuration, live projectile framing, deferred and exactly-once damage/10 MP,
hit recoil, pause/resume, visual fallback, insufficient MP, mid-flight disable,
invalidated targets, battle-end cancellation, healing regression, lethal-hit
death tails, and resource cleanup. A queued old player turn and late skill/item
cancellation could not reopen commands after the resulting victory. Native
timeline LateUpdate remained enabled. No Unity errors or destroyed-target tween
warnings were reported. Flight and impact captures were inspected at 1280x720.

The 32 enemy Fire assertions were rerun and passed, totaling 60 checks for this
extension. The earlier 57 boss-rotation and 20 timeline-lifecycle suites were not
rerun for this player-only hookup. Helpers and results are archived under ignored
`Library/FireBallFeedbackChecks` and `Library/FireFeedbackChecks`, not Assets.
No new runtime script or material was needed; FireBall data, the shared component,
material, and user scene hashes stayed unchanged. Standalone-build acceptance
and user approval of the player's visual timing remain outstanding.

## 中文

### 范围

现有敌方 `Enemy/Fire.asset` 和玩家 `FireBall.asset` 会发射带黄色内核和短促渐隐拖尾的橙色火球。
命中时产生一小团火星，并调用一次原来的 `UseSkill`；之后继续沿用现有伤害飘字、
受击晃动、死亡处理和 HP 事件。

启用条件是 `BattleFireFeedback` 上的技能资源引用：Enemy Prefab 引用 Fire，Player Prefab
引用 FireBall。双方复用同一个未修改的组件、材质、贴图和粒子参数；名称相同的其他技能
不会匹配。Burn、MagmaBurst、治疗、道具、技能数据和 Boss AI 均不变。
本次不加入音效、模型闪白、动态灯光或通用技能特效框架。

### 配置与时机

`BattleFireFeedback` 引用角色控制器、该 Prefab 的 Fire 或 FireBall 资源和 `BattleFireParticles.mat`。
该材质沿用死亡粒子已经使用的受支持内置粒子着色器和柔边贴图，不修改原有资源，
也不创建材质副本。

默认飞行 0.5 秒，命中粒子收尾 0.3 秒，火球直径为 0.65 个世界单位。
火球从施法者附近出发，沿轻微弧线飞向选定目标碰撞体的上部，避免火星埋在方块中心
看不见；没有碰撞体时使用一个小的高度偏移。不靠碰撞或射线判定伤害，火球始终跟随
最初选定的角色引用。

`BattleManager.PlayEnemyActionSequence`（Fire）和 `PlaySkillSequence`（FireBall）
会等待原有交互镜头到达，再发射火球。
到达回调执行原技能和战斗结束检查，保留现有伤害公式和 MP 消耗路径。
当前 Boss 的 Fire 仍为 70 攻击力 + 20 技能强度 = 90 伤害，消耗 0 MP。
玩家 FireBall 保留攻击力 + 20 的伤害，命中时只消耗一次 10 MP。
行动会等待命中粒子收尾；镜头在 `finally` 中解锁，提前退出也会解锁。
其他技能保留原有路径和镜头选择。

### 取消与资源

飞行、粒子和命中等待均使用游戏时间，暂停时一起停止。施法者或目标死亡、失活或销毁
时取消尚未发生的命中，不会换成补位敌人或复活后的模型。管理器在执行技能前也会再次
检查战斗状态和 MP。目标被此次命中打死，不会提前切掉命中粒子的收尾。拒绝重复播放。

粒子材质缺失时回退原技能回调；缺少或禁用反馈组件时，使用原技能表现流程。
飞行中关闭组件会取消那次尚未发生的命中。所有粒子系统都没有碰撞体，使用共享材质，
每个系统最多 64 个粒子。正常结束、取消、组件关闭/销毁和场景卸载都会清理所属运行时
特效。特效不会修改角色 Transform、源材质、场景或真实存档。

### 敌方 Fire 验证（2026-11-03）

2026-11-03，将落点调整到模型上部后，32 项专项 Unity Play Mode 断言通过。
使用真实 Enemy Prefab、Fire 资源、材质和镜头，并保持原生时间轴 LateUpdate 启用。
覆盖延迟且只执行一次的伤害/费用、原有 90 伤害/0 MP、暂停/恢复、镜头构图与解锁、
活动粒子、拒绝重复播放、显示回退、飞行中禁用、目标销毁或死亡、施法者死亡、致命命中
收尾和运行时清理。额外使用仅存在于运行时的收费技能副本验证消耗时机，没有修改实际
Fire。Burn 和普通 Attack 保留原有行为；该次没有 Unity 错误或已销毁目标的动画警告。

另外重跑了既有 57 项 Boss 轮转和 20 项时间轴生命周期检查，均通过，本次共 109 项。
已检查真实战斗镜头的 1280x720 飞行与命中截图。临时助手、结果和图片归档忽略的
Library 目录，不留在 Assets 中随游戏提交。源场景以及 Fire/MagmaBurst 资源哈希保持
不变。独立构建和用户对视觉节奏的确认仍待验收。

### 玩家 FireBall 后续接入（2026-11-04）

使用真实 Player Prefab、FireBall 资源、技能确认协程和战斗镜头的 28 项新增 Play Mode
断言通过，覆盖共享资源配置、活动火球构图、延迟且只执行一次的伤害/10 MP、受击晃动、
暂停/恢复、显示回退、MP 不足、飞行中禁用、目标失效、结算时取消命中、治疗回归、
致命命中的死亡收尾和资源清理。排队等待的旧玩家回合和延迟触发的技能/道具取消，
都不能在此次胜利后重新打开指令菜单。原生时间轴 LateUpdate 保持启用；没有 Unity
错误或已销毁目标的动画警告。已检查 1280x720 的飞行和命中截图。

另外重跑的 32 项敌方 Fire 检查通过，本次扩展共 60 项。此前 57 项 Boss 轮转和 20 项
时间轴生命周期检查没有为此次仅接入玩家侧的改动重跑。临时助手和结果归档忽略的
`Library/FireBallFeedbackChecks` 与 `Library/FireFeedbackChecks`，不留在 Assets。
没有新增运行时脚本或材质；FireBall 数据、共用组件、材质和用户场景的哈希保持不变。
独立构建验收和用户对玩家侧视觉节奏的确认仍待进行。
