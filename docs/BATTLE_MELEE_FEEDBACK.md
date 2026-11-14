# Animated Melee Feedback / 骨骼动画近战反馈

## English

### Scope and Flow

Argo's normal Attack now blinks to the chosen enemy, strikes once, and blinks back
to its original formation position. The opt-in is on `Argo_Lycaon.prefab` only.
Paul, enemy attacks, ranged skills and character data keep their existing rules.
This work follows the separate model/Idle checkpoint `beb538b6`; it does not
replace or amend that commit.

`BattleAttackFeedback` delegates to optional `BattleMeleeFeedback`. Without that
component, the original cube lunge is unchanged. The melee component owns four
phases: Idle, Approach, Strike and Return. It restores the exact original display
position and rotation on completion or cancellation.

Contact is computed from the target collider's closest point, with clearance for
the actor's collider radius and a 0.15-unit gap. The actor does not teleport into the
target centre or move a fixed short distance. Already-close targets skip travel.
On approach and return the display renderers briefly hide for `blinkTime` (0.06
seconds by default), then the display snaps directly to its destination. There is
no position tween or intermediate running. Visibility is restored to each
renderer's previous enabled state, including on cancellation. The contact point
is refreshed after the approach blink in case the target moved.
Only the collider-free display hierarchy teleports; the gameplay root, formation
slot, targeting collider and timeline identity remain at their original position.
This is presentation-only teleportation, not a new physics or pathfinding system.

### Clips and Hit Timing

The Animator still contains Idle, Run and Attack. Battle melee only uses Attack
and Idle; Run and its looping animation asset are retained for possible future
Field integration, but no Field animation controller is added in this change.
The controller does not require transition arrows or parameters. Attack plays
once, followed by a blink home and Idle.

The native Run/Attack clips come from the previously inspected local exports.
Their `Bip001` horizontal position curves are held at the Idle reference, preserving
bone rotations and vertical poses while removing the exported forward travel.
Animator root motion stays disabled. This separates posing from movement:
animation changes the bones, while code teleports the display toward the enemy.
No new source FBX or textures are imported for this step.

The current first kick uses `impactNormalizedTime = 0.1333333`, selected from the
first near-maximum forward foot extension in the clip (about 0.14 seconds into the
strike). This value is editable on `BattleMeleeFeedback`; it is not an Animation
Event or a new damage formula. The existing BattleManager callback applies Attack
damage once at that moment. There is no MP cost or change to skill calculations.

### Cameras and Cleanup

`BattleCameraDirector` frames both formation and contact positions for Approach, moves into a
contact shot at arrival, and restores the cached travel shot for Return. The
cached shot remains usable if the target dies or is destroyed. Bounds fitting
accounts for viewport aspect ratio and actor height. Enemy floating text is raised
from local Y 0.4 to 0.8 so the cube does not hide the damage number in the close shot.

`BattleManager` waits for animated melee recovery before resolving a final-enemy
victory. Enemy death feedback and reserve spawning still finish as before. The
result is checked again after recovery; camera ownership is released in `finally`.
This does not delay a defeat or change rewards.

Losing a target before impact skips damage and returns the living actor home.
After a lethal impact, target lifetime no longer controls recovery. Disabling
either feedback component, deactivating/destroying the actor, or losing its living
state cancels the action and restores the display and visibility. Pause freezes
both animation and the blink delay. Dedicated hit/cast/death animations and sound
effects are outside this step.

### Verification

The original run-based implementation passed 68 focused Unity Play Mode assertions
on 2026-11-12, covering real spawning,
true approach and return, left/right and larger targets, already-close targets,
single unchanged damage, stable gameplay roots, repeated actions, pause/resume,
component/actor disable, attacker death, target destruction, scene unload, Paul
fallback, unchanged FireBall damage/MP, and victory after recovery. Screenshots and
complete-bound framing checks cover 1280x720 and 800x600. No captured Unity errors,
exceptions or DOTween warnings remained. Test saves use an isolated directory.
Reports, screenshots and one-off tooling are archived under ignored
`Library/MeleeChecks`; no standalone build or full-demo playthrough is claimed.

The 2026-11-13 blink revision passed 64 focused Unity Play Mode assertions:
endpoint-only teleportation without Run, single unchanged damage at contact,
exact home pose and renderer-state recovery, left/right and large/close targets,
pause/resume, component disable, attacker death/deactivation, target loss/movement,
unchanged Paul and FireBall behavior, and victory after the visible actor returns.
1280x720 and 800x600 impact screenshots were inspected. The forced-unload fixture
waits for result-panel fades to finish first; immediate result-UI teardown is not
covered. No captured Unity exceptions, errors or tween warnings remained in the
final run. Run clip/controller, all three scenes, BattleManager and camera code
were not edited for this revision. The check script, reports and captures are
archived under ignored `Library/BlinkMeleeChecks`, not shipped as game assets.

### Planned Visual Follow-up

The 2026-11-14 direction is a brief horizontal-streak afterimage with a softened,
dissolving silhouette for departure and arrival, not a large portal or explosion.
This is deferred to a separate change. The current checkpoint includes blink
movement and attack animation only, without the new teleport effect or imported
VFX packages.

## 中文翻译

### 范围与流程

Argo 的普通 Attack 现在会瞬移到选中的敌人面前，攻击一次，再瞬移回原来的阵型位置。
只有 `Argo_Lycaon.prefab` 启用这套反馈。Paul、敌人的攻击、远程技能和角色数据保持
原来的规则。本次基于单独的模型/待机提交 `beb538b6` 继续开发，没有替换或修改那笔提交。

`BattleAttackFeedback` 将动画近战交给可选的 `BattleMeleeFeedback`；没有这个组件时，
方块仍采用原来的前探。近战组件管理 Idle、Approach、Strike、Return 四个阶段，完成或
取消后会精确恢复显示层原来的位置和旋转。

停靠点根据目标碰撞体的最近表面计算，并为行动者的碰撞体半径和 0.15 个单位的间隙留出
距离，不会直接瞬移进敌人中心，也不是固定前探一小段。已经足够近的目标会跳过移动。
接近和返回时，显示层的 Renderer 会短暂隐藏 `blinkTime`（默认 0.06 秒），然后直接
切换到目的地，不做位置补间，也不再播放跑步。每个 Renderer 都会恢复原有的启用状态，
中断时也会恢复。接近闪隐结束后会重新计算停靠点，以适应目标位置变化。
实际瞬移的是没有碰撞体的显示层；
逻辑根节点、阵型槽位、点击碰撞体和时间轴身份留在原位置。这是平坦战斗舞台上的
表现瞬移，没有引入新的物理或寻路系统。

### 动画与命中时刻

Animator 仍包含 Idle、Run、Attack。战斗近战只使用 Attack 和 Idle；保留 Run 状态和
循环跑步动画资源，方便以后接入 Field，但本次没有添加 Field 动画控制器。不需要给
控制器额外画过渡箭头或添加参数。Attack 播放一次，然后瞬移回家并恢复 Idle。

Run/Attack 原生动画来自之前已经检查过的本地导出。它们的 `Bip001` 水平位置曲线固定
在 Idle 的参考位置，保留关节旋转和竖直姿态，去掉源动画自带的向前移动。
Animator 不应用根运动，从而将姿态和位移分开：动画改变骨骼，代码把显示层瞬移到
敌人。本次没有额外导入源 FBX 或贴图。

当前第一下踢击的 `impactNormalizedTime = 0.1333333`，取自动画中脚部首次接近最远
前伸的位置，即 Strike 开始后约 0.14 秒。这个数值可在 `BattleMeleeFeedback` 中调整，
不是 Animation Event，也不是新的伤害公式。仍由 BattleManager 原来的回调在该时刻
结算一次 Attack 伤害，不增加 MP 消耗，也不改变技能计算。

### 镜头与收尾

`BattleCameraDirector` 在接近时框住阵型与停靠位置，到达后切到交战近景，返回时恢复缓存
的全程镜头。即使目标阵亡或被销毁，缓存镜头也仍可用于返回。取景计算考虑画面宽高比和
人物高度。敌方飘字锚点从局部 Y 0.4 抬到 0.8，避免交战近景中方块挡住伤害数字。

最后一个敌人被动画近战击倒时，`BattleManager` 会等行动者归位再判定胜利。
敌人的死亡反馈和后备敌人生成仍按原流程完成，归位后再次检查结果，并在 `finally` 中
释放镜头。该保护不延后败北，也不修改奖励。

命中前失去目标时跳过伤害，存活的行动者会返回原位；命中之后，目标是否还存在不再影响
归位。禁用任一反馈组件、停用或销毁行动者、或行动者失去存活状态，都会取消移动并恢复
显示层与可见性。暂停会同时冻结动画和闪隐计时。本次没有增加专门的受击、施法、死亡
动画或音效。

### 验证

原跑步版本在 2026-11-12 的 Unity Play Mode 针对性检查有 68 项断言通过，覆盖真实生成、实际跑近与
返回、左右方向和较大目标、近距离目标、单次且数值不变的伤害、稳定的逻辑根节点、重复
动作、暂停恢复、组件/角色禁用、攻击者死亡、目标销毁、场景卸载、Paul 的原反馈、
FireBall 的原伤害/MP，以及归位后才结算胜利。1280x720 和 800x600 均检查了截图和
完整包围盒取景，没有遗留捕获到的 Unity 错误、异常或 DOTween 警告。测试存档使用隔离
目录。报告、截图和一次性工具归档在忽略的 `Library/MeleeChecks` 中；本次不声称完成
独立构建或整个 Demo 的完整游玩测试。

2026-11-13 的瞬移版本通过了 64 项 Unity Play Mode 针对性断言：只在起终点之间瞬移且
不播放 Run、停靠点结算一次原有伤害、精确恢复原位姿态及 Renderer 状态、左右方向和
较大/较近目标、暂停恢复、组件禁用、攻击者死亡或停用、目标消失或移动、Paul 和
FireBall 的原有行为，以及角色可见地归位后才结算胜利。已检查 1280x720 和 800x600
的命中截图。强制卸载测试会先等待结算面板的淡入淡出结束，不覆盖结算 UI 立即销毁的
情形。最终运行没有捕获到 Unity 异常、错误或补间警告。本次瞬移调整没有修改 Run
动画/控制器、三个场景、BattleManager 或镜头代码。检查脚本、报告与截图归档在忽略的
`Library/BlinkMeleeChecks` 下，不作为游戏资源上传。

### 后续特效方向

2026-11-14 确认的方向是：离开和出现时，带有短促的横向线条残影，人物轮廓虚化消散，
而不是大型传送门或爆炸。该特效留作单独的后续改动。当前提交节点只包含瞬移位移和
攻击动画，不包含新的瞬移特效，也没有导入第三方特效包。
