# MagmaBurst Feedback / MagmaBurst 裂痕与火柱

Updated / 更新：2026-11-07

## English

### Appearance and Timing

The existing enemy `MagmaBurst` now opens branching orange/yellow cracks below
each living opposing target. After a 0.35-second crack windup, flame columns and
rising sparks erupt around all targets together. The original group skill resolves
once at eruption, followed by a 0.85-second rising/fading tail and the existing
result hold. The camera retains the existing caster shot, then uses the player-group
viewing direction with the entire group effect centered and fitted in frame.

Uncharged damage remains 70 Attack + 10 power = 80 per target; charged damage
remains 120 per target at the existing 1.5 multiplier. The actual skill still costs
0 MP, retains its cooldown and dangerous marker, and uses the original boss AI
and phase rotation. This visual does not apply Burn or introduce a new mechanic.

### Configuration

`BattleMagmaFeedback` is attached to `Enemy.prefab`, including the boss spawned
from that prefab. It matches the original MagmaBurst resource by identity, not
display name. Fire, FireBall, Burn, healing, and ordinary attacks do not opt in.
No SkillData fields or skill assets were added or changed.

The component references two new shared materials, `BattleMagmaCracks` and
`BattleMagmaColumn`, both using `Battle/Magma`, plus the existing unchanged soft
`BattleFireParticles` material for sparks. The new shader animates flame streaks,
uneven tongues, and vertical bands using a per-cast time property; it does not
depend on an unscaled global clock. Ground cracks have seven jagged main paths,
small branches, orange outer ribbons, and narrow yellow cores.

Each cast creates two owned meshes shared among all target effects. There are no
material clones, dynamic lights, gameplay colliders, or model tint/scale changes.
Each target's spark system emits 28 particles with a 48-particle cap. The crack
and column scale comes from the target's logical collider bounds, with a minimum
radius of 0.8 and height of 2.8 world units; cracks extend beyond the cube footprint.
Effects are world-space visuals placed just above the initial feet position.

### Integration and Cleanup

`BattleManager.PlayGroupSkillSequence` keeps group target selection and camera
movement. Only the matched effect uses `BattleCameraDirector.FocusGroupEffectShot`
with its presentation bounds: the shot keeps the configured player-group rotation,
fits all eight bounds corners using the camera FOV/aspect, and adds 20 percent
distance margin. Other group skills keep their previous shots. No source shot
transform or scene is saved or overwritten. After the camera arrives, the component receives the
selected targets and a callback. The callback rechecks battle end, caster life,
and MP, then calls the original `UseSkillOnTargets` once and the original result
checks. MP is never charged separately by the visual or once per player.
The existing group fallback still executes the same callback directly.
The camera unlocks in `finally` on normal completion and early exits.

The feedback removes duplicate targets, ignores dead/inactive/opposing-team
mismatches, and permanently excludes a selected target lost during the windup.
It does not replace a lost unit with a newly spawned or revived unit. Losing all
targets or the caster before eruption cancels pending impact. Disabling the
component mid-cast also cancels it. After impact, lethal targets retain the tail
so defeat does not abruptly erase the eruption.

Scaled game time controls crack growth, flame rise/fade, and particles. Pausing
freezes presentation and pending impact. Reentrant playback is rejected.
Missing crack/column materials fall back to the original callback; missing sparks
material leaves mesh visuals and damage intact. A missing/disabled component uses
the manager's original group fallback. Completion, cancellation, disable,
destruction, and scene unload remove particle roots and generated meshes.

### Verification (2026-11-07)

43 focused Unity Play Mode assertions passed using the real Enemy/Player prefabs,
MagmaBurst asset, camera and native timeline LateUpdate. They cover visible cracks
before damage, two-target simultaneous hits, original 80/120 damage and 0 MP,
exactly-once group cost with a paid runtime copy, mid-windup MP loss, pause/resume,
identity matching, duplicate rejection, visual fallback, component disable,
target/caster loss, late battle-end cancellation, lethal tails, mesh cleanup,
and unrelated PartyHeal/group Debuff behavior. No Unity exceptions, shader errors,
or destroyed-target tween warnings were reported in the final run.

Actual battle-camera captures at 1280x720 and 800x600 were inspected. Pixel checks
verify orange cracks/fire and tests confirm all column bounds remain in frame.
The 57 boss-rotation and 20 timeline-lifecycle checks were also rerun and passed
after the final camera change, totaling 120 assertions. Helpers, reports, and images
are archived in ignored `Library/MagmaFeedbackChecks`, `Library/BossRotationChecks`,
and `Library/TimelineLifecycleChecks` directories,
not included as game Assets. Source scene, MagmaBurst, enemy database, and real save
files were not changed by the tests. A standalone player build and the user's
visual/timing acceptance remain pending.

## 中文翻译

### 外观与时机

现有敌方 `MagmaBurst` 会在每个存活敌对目标脚下展开橙黄色分叉裂痕。
裂痕预热 0.35 秒后，所有目标周围同步升起火焰柱和上升火星。
原群体技能在喷发瞬间统一执行一次，之后播放 0.85 秒的上升/淡出收尾，
再保留原结果停留时间。镜头沿用原施法者特写，再使用原玩家组镜头方向，
将整组特效居中并调整到能完整容纳的距离。

未蓄力伤害仍为 70 攻击力 + 10 技能强度 = 每人 80；沿用原 1.5 倍倍率后，
蓄力版仍为每人 120。实际技能仍消耗 0 MP，保留原冷却、危险标记、
Boss AI 和转阶段轮转。这套表现不会附加 Burn，也没有新增机制。

### 配置

`BattleMagmaFeedback` 挂在 `Enemy.prefab` 上，由该 Prefab 生成的 Boss 同样具备。
组件按原 MagmaBurst 技能资源身份匹配，不按显示名匹配。
Fire、FireBall、Burn、治疗和普通攻击不会自动套用；没有新增或修改 SkillData 字段及技能资源。

组件引用两个新共享材质 `BattleMagmaCracks`、`BattleMagmaColumn`，共用 `Battle/Magma`
着色器；火星复用未修改的柔边 `BattleFireParticles` 材质。
新着色器使用每次施法传入的时间参数制作流动焰纹、不规则火舌和纵向明暗，
不依赖不受暂停控制的全局时钟。地面裂痕由七条曲折主干、小分支、橙色外层和细黄色内核构成。

每次施法创建两个自有网格，供所有目标表现共享；不生成材质副本、动态灯光或玩法碰撞体，
也不改变模型颜色/缩放。每个目标的火星系统发射 28 粒，上限 48。
裂痕和火柱按逻辑碰撞体范围调整，最小半径 0.8、高度 2.8 个世界单位，
裂痕伸出方块占地以保持可见。特效位于初始脚下略高的位置，使用世界空间，不跟着模型晃动。

### 接入与清理

`BattleManager.PlayGroupSkillSequence` 保留群体目标选择和镜头移动。
仅匹配该特效时调用 `BattleCameraDirector.FocusGroupEffectShot`，使用表现范围计算构图：
保留配置的玩家组镜头方向，按相机视野角和宽高比容纳范围的八个角，再增加 20% 距离余量。
其他群体技能镜头不变，不保存或覆盖源镜头 Transform 或场景。
镜头到位后，将已选目标及回调传给反馈组件。
回调再次检查战斗结束、施法者存活及 MP，然后只调用一次原 `UseSkillOnTargets`
和原结算检查。表现组件不单独扣费，也不按目标人数重复扣费；原群体回退路径直接执行同一回调。
正常结束和提前退出均通过 `finally` 解锁镜头。

反馈移除重复目标，忽略死亡、失活或阵营不符的对象，并排除预热中失效的已选单位，
不会用新生成或复活的单位替换。喷发前施法者或全部目标失效时取消尚未发生的命中；
中途禁用组件也会取消。命中之后允许致死目标保留火柱收尾，避免失败结算突然切掉喷发。

裂痕展开、火柱升起/淡出及粒子均使用游戏缩放时间，暂停会冻结表现和待执行命中。
拒绝重复进入播放。缺少裂痕/火柱材质时回退原技能回调；缺少火星材质时仍保留网格表现和伤害。
缺少或禁用组件时走管理器原群体回退路径。
结束、取消、禁用、销毁及场景卸载都会清理粒子根节点和生成的网格。

### 验证（2026-11-07）

43 项专项 Unity Play Mode 断言通过，使用真实 Enemy/Player Prefab、MagmaBurst、
镜头和原生时间轴 LateUpdate。覆盖扣血前可见裂痕、双目标同步命中、原 80/120 伤害与 0 MP、
通过运行时收费副本验证群体只扣一次 MP、预热中失去 MP、暂停/恢复、资源身份匹配、
重复播放拒绝、视觉回退、禁用、目标/施法者失效、结算后取消延迟伤害、致死收尾、
网格清理和无关的 PartyHeal/群体 Debuff 行为。最终检查没有 Unity 异常、Shader 错误或销毁目标动画警告。

已检查 1280x720 和 800x600 的实际战斗镜头截图，通过像素检查验证橙色裂痕/火焰，
并检查所有火柱范围均在画面内。最终镜头修改后重跑的 57 项 Boss 轮转和 20 项时间轴生命周期检查也通过，
本轮共 120 项。临时脚本、报告和图片归档忽略的 `Library/MagmaFeedbackChecks`、
`Library/BossRotationChecks`、`Library/TimelineLifecycleChecks`，不作为游戏 Assets 提交。
测试未改源场景、MagmaBurst、敌人数据库或真实存档。独立 Player 构建以及用户对外观/节奏的确认仍待验收。
