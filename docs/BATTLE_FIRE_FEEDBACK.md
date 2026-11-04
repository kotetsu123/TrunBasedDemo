# Fire Projectile Feedback / Fire 火球反馈

## English

### Scope

The enemy's existing `Enemy/Fire.asset` now launches an orange fireball with a
yellow core and a short fading trail. On contact, it emits a small spark burst
and calls the original `UseSkill` once. Existing damage text, hit recoil, death
handling, and HP events remain downstream of that call.

The opt-in is an asset reference on `BattleFireFeedback`, configured on the shared
Enemy prefab. Bosses and ordinary enemies using that exact Fire asset receive
the effect. A skill with the same name does not match. Player `FireBall`, Burn,
MagmaBurst, healing, items, skill data, and boss AI are unchanged. This does not
add audio, model flashing, dynamic lights, or a general skill-effect framework.

### Configuration and Timing

`BattleFireFeedback` holds the controller, Fire asset, and
`BattleFireParticles.mat`. The material uses the same supported built-in particle
shader and existing soft texture as the death particles, without editing those
assets or allocating material copies.

Defaults are 0.5 seconds of flight, 0.3 seconds of impact/particle recovery, and a
0.65-world-unit projectile diameter. The shot starts near the caster and travels
along a shallow arc toward the selected target's upper collider bounds. This
keeps sparks visible instead of burying them at the center of the cube. Without
a collider, a small height offset is used. No collision or raycast decides damage;
the projectile follows the originally selected character reference.

`BattleManager.PlayEnemyActionSequence` waits for the existing interaction camera
to arrive before launching Fire. The arrival callback executes the original
skill and battle-end check, retaining the current damage formula and MP path.
For the current boss, Fire remains 70 Attack + 20 power = 90 damage and 0 MP.
The action waits for the impact tail; the camera unlocks in `finally`, including
early exits. Other enemy skills retain their original paths.

### Cancellation and Resources

Flight, particles, and impact waits use scaled game time and pause together.
Dead, inactive, or destroyed casters/targets cancel pending impact; a replacement
enemy or revived model is not substituted. The manager also rechecks battle and
MP state before applying the skill. Target death caused by the hit does not cut
off the impact tail. Duplicate playback is rejected.

Missing particle material falls back to the original skill callback; missing or
disabled feedback uses the existing enemy skill presentation. Disabling feedback
during flight cancels that pending impact. Every particle system is collider-free,
uses the shared material, and has a 64-particle limit. Completion, cancellation,
component disable/destruction, and scene unload remove the owned runtime effect.
No actor transform, source material, scene, or real save is edited by the effect.

### Verification

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

## 中文

### 范围

敌人现有的 `Enemy/Fire.asset` 现在会发射带黄色内核和短促渐隐拖尾的橙色火球。
命中时产生一小团火星，并调用一次原来的 `UseSkill`；之后继续沿用现有伤害飘字、
受击晃动、死亡处理和 HP 事件。

启用条件是 `BattleFireFeedback` 上的技能资源引用，已经配置到共用 Enemy Prefab。
Boss 和普通敌人只要使用这个确切的 Fire 资源，就会获得该效果；名称相同的其他技能
不会匹配。玩家 `FireBall`、Burn、MagmaBurst、治疗、道具、技能数据和 Boss AI 均不变。
本次不加入音效、模型闪白、动态灯光或通用技能特效框架。

### 配置与时机

`BattleFireFeedback` 引用角色控制器、Fire 资源和 `BattleFireParticles.mat`。
该材质沿用死亡粒子已经使用的受支持内置粒子着色器和柔边贴图，不修改原有资源，
也不创建材质副本。

默认飞行 0.5 秒，命中粒子收尾 0.3 秒，火球直径为 0.65 个世界单位。
火球从施法者附近出发，沿轻微弧线飞向选定目标碰撞体的上部，避免火星埋在方块中心
看不见；没有碰撞体时使用一个小的高度偏移。不靠碰撞或射线判定伤害，火球始终跟随
最初选定的角色引用。

`BattleManager.PlayEnemyActionSequence` 会等待原有交互镜头到达，再发射 Fire。
到达回调执行原技能和战斗结束检查，保留现有伤害公式和 MP 消耗路径。
当前 Boss 的 Fire 仍为 70 攻击力 + 20 技能强度 = 90 伤害，消耗 0 MP。
行动会等待命中粒子收尾；镜头在 `finally` 中解锁，提前退出也会解锁。
其他敌方技能继续走原有路径。

### 取消与资源

飞行、粒子和命中等待均使用游戏时间，暂停时一起停止。施法者或目标死亡、失活或销毁
时取消尚未发生的命中，不会换成补位敌人或复活后的模型。管理器在执行技能前也会再次
检查战斗状态和 MP。目标被此次命中打死，不会提前切掉命中粒子的收尾。拒绝重复播放。

粒子材质缺失时回退原技能回调；缺少或禁用反馈组件时，使用原敌方技能表现流程。
飞行中关闭组件会取消那次尚未发生的命中。所有粒子系统都没有碰撞体，使用共享材质，
每个系统最多 64 个粒子。正常结束、取消、组件关闭/销毁和场景卸载都会清理所属运行时
特效。特效不会修改角色 Transform、源材质、场景或真实存档。

### 验证

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
